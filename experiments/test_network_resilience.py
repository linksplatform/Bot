#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Test script to verify network resilience improvements.

This script tests the network handler implementation to ensure it properly
handles connection failures, timeouts, and network changes.
"""

import sys
import os
import time
import unittest
from unittest.mock import patch, MagicMock
import requests

# Add python module path
sys.path.insert(0, os.path.join(os.path.dirname(__file__), '..', 'python'))

try:
    from network_handler import NetworkHandler, VkNetworkMixin
except ImportError as e:
    print(f"Import error: {e}")
    sys.exit(1)


class TestNetworkHandler(unittest.TestCase):
    """Test cases for NetworkHandler."""
    
    def setUp(self):
        """Set up test cases."""
        self.handler = NetworkHandler(max_retries=3, backoff_factor=0.1)
        self.session = self.handler.create_session()
    
    def test_session_creation(self):
        """Test that session is created with proper configuration."""
        self.assertIsInstance(self.session, requests.Session)
        self.assertEqual(self.session.timeout, (10, 30))  # Connection, read timeout
    
    def test_connection_stats(self):
        """Test connection statistics tracking."""
        stats = self.handler.get_connection_stats()
        self.assertIn('is_connected', stats)
        self.assertIn('connection_failures', stats)
        self.assertIn('last_successful_request', stats)
        self.assertTrue(stats['is_connected'])
        self.assertEqual(stats['connection_failures'], 0)
    
    @patch('requests.Session.request')
    def test_successful_request(self, mock_request):
        """Test successful request handling."""
        mock_response = MagicMock()
        mock_response.status_code = 200
        mock_response.json.return_value = {'response': 'success'}
        mock_request.return_value = mock_response
        
        response = self.handler.make_request(self.session, 'POST', 'https://api.vk.com/method/test')
        
        self.assertEqual(response, mock_response)
        self.assertTrue(self.handler.is_connected())
    
    @patch('requests.Session.request')
    def test_connection_error_handling(self, mock_request):
        """Test connection error handling and retry logic."""
        mock_request.side_effect = requests.exceptions.ConnectionError("Connection failed")
        
        with self.assertRaises(requests.exceptions.ConnectionError):
            self.handler.make_request(self.session, 'POST', 'https://api.vk.com/method/test')
        
        self.assertFalse(self.handler.is_connected())
        self.assertGreater(self.handler._connection_failures, 0)
    
    @patch('requests.Session.request')
    def test_timeout_error_handling(self, mock_request):
        """Test timeout error handling."""
        mock_request.side_effect = requests.exceptions.Timeout("Request timed out")
        
        with self.assertRaises(requests.exceptions.Timeout):
            self.handler.make_request(self.session, 'POST', 'https://api.vk.com/method/test')
    
    def test_health_check_start_stop(self):
        """Test health check monitoring start/stop."""
        self.handler.start_health_check()
        self.assertTrue(self.handler._health_check_thread.is_alive())
        
        self.handler.stop_health_check()
        time.sleep(0.1)  # Give time for thread to stop
        self.assertFalse(self.handler._health_check_thread.is_alive())


class MockVk:
    """Mock VK class for testing."""
    
    def __init__(self, token, group_id, debug=False, api='5.131'):
        self.token = token
        self.group_id = group_id
        self.debug = debug
        self.api_version = api
    
    def start_listen(self):
        """Mock start_listen method."""
        pass


class TestVkNetworkMixin(unittest.TestCase):
    """Test cases for VkNetworkMixin."""
    
    def setUp(self):
        """Set up test case."""
        
        # Create a test class that combines the mixin with mock VK
        class TestBot(VkNetworkMixin, MockVk):
            def __init__(self, *args, **kwargs):
                super().__init__(*args, **kwargs)
        
        self.bot = TestBot(token='test_token', group_id=12345)
    
    def test_mixin_initialization(self):
        """Test that mixin initializes properly."""
        self.assertIsInstance(self.bot.network_handler, NetworkHandler)
        self.assertIsNotNone(self.bot._vk_session)
    
    @patch('requests.Session.request')
    def test_call_method_success(self, mock_request):
        """Test successful API method call."""
        mock_response = MagicMock()
        mock_response.status_code = 200
        mock_response.json.return_value = {'response': [{'id': 1, 'first_name': 'Test'}]}
        mock_request.return_value = mock_response
        
        result = self.bot.call_method('users.get', {'user_ids': 1})
        
        self.assertEqual(result, {'response': [{'id': 1, 'first_name': 'Test'}]})
        mock_request.assert_called_once()
    
    @patch('requests.Session.request')
    def test_call_method_vk_api_error(self, mock_request):
        """Test VK API error handling."""
        mock_response = MagicMock()
        mock_response.status_code = 200
        mock_response.json.return_value = {
            'error': {
                'error_code': 5,
                'error_msg': 'User authorization failed'
            }
        }
        mock_request.return_value = mock_response
        
        result = self.bot.call_method('users.get', {'user_ids': 1})
        
        # Should return the error response, not raise exception
        self.assertIn('error', result)
        self.assertEqual(result['error']['error_code'], 5)
    
    @patch('requests.Session.request')
    def test_call_method_network_error(self, mock_request):
        """Test network error handling in call_method."""
        mock_request.side_effect = requests.exceptions.ConnectionError("Network error")
        
        with self.assertRaises(requests.exceptions.ConnectionError):
            self.bot.call_method('users.get', {'user_ids': 1})


def simulate_network_recovery():
    """Simulate network recovery scenario."""
    print("\n=== Simulating Network Recovery Scenario ===")
    
    handler = NetworkHandler(max_retries=2, backoff_factor=0.1)
    session = handler.create_session()
    
    call_count = 0
    
    def failing_then_success(*args, **kwargs):
        nonlocal call_count
        call_count += 1
        if call_count <= 2:  # First two calls fail
            raise requests.exceptions.ConnectionError(f"Network failure #{call_count}")
        else:  # Third call succeeds
            mock_response = MagicMock()
            mock_response.status_code = 200
            mock_response.json.return_value = {'response': 'success'}
            return mock_response
    
    with patch.object(requests.Session, 'request', side_effect=failing_then_success):
        try:
            print("Testing network recovery with 2 failures followed by success...")
            response = handler.make_request(session, 'POST', 'https://api.vk.com/method/test')
            print(f"✓ Recovery successful after {call_count} attempts")
            print(f"✓ Connection status: {handler.is_connected()}")
            print(f"✓ Failure count: {handler._connection_failures}")
        except Exception as e:
            print(f"✗ Recovery failed: {e}")


def simulate_intermittent_failures():
    """Simulate intermittent network failures."""
    print("\n=== Simulating Intermittent Failures ===")
    
    handler = NetworkHandler(max_retries=3, backoff_factor=0.1)
    session = handler.create_session()
    
    success_count = 0
    failure_count = 0
    
    def intermittent_failure(*args, **kwargs):
        import random
        if random.random() < 0.3:  # 30% success rate
            mock_response = MagicMock()
            mock_response.status_code = 200
            mock_response.json.return_value = {'response': 'success'}
            return mock_response
        else:
            raise requests.exceptions.ConnectionError("Intermittent failure")
    
    with patch.object(requests.Session, 'request', side_effect=intermittent_failure):
        for i in range(10):
            try:
                handler.make_request(session, 'POST', 'https://api.vk.com/method/test')
                success_count += 1
                print(f"Request {i+1}: ✓ Success")
            except Exception:
                failure_count += 1
                print(f"Request {i+1}: ✗ Failed after retries")
    
    print(f"\nResults: {success_count} successes, {failure_count} final failures")
    print(f"Connection status: {handler.is_connected()}")


if __name__ == '__main__':
    print("=== VK Bot Network Resilience Test Suite ===")
    
    # Run unit tests
    print("\n1. Running Unit Tests:")
    unittest.main(verbosity=2, exit=False, argv=[''])
    
    # Run simulation tests
    simulate_network_recovery()
    simulate_intermittent_failures()
    
    print("\n=== Test Suite Completed ===")