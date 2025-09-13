# -*- coding: utf-8 -*-
"""Network error handling and reconnection logic for VK Bot.

This module provides robust network error handling, automatic reconnection,
and connection health monitoring for the VK bot to handle network changes
and connection losses gracefully.
"""

import time
import logging
import threading
from typing import Any, Dict, Optional, Callable
from datetime import datetime, timedelta
import requests
from requests.adapters import HTTPAdapter
from requests.packages.urllib3.util.retry import Retry


class NetworkHandler:
    """Handles network connectivity issues and provides reconnection logic."""
    
    def __init__(
        self,
        max_retries: int = 5,
        backoff_factor: float = 0.5,
        retry_status_codes: tuple = (429, 500, 502, 503, 504),
        connection_timeout: int = 10,
        read_timeout: int = 30,
        health_check_interval: int = 60
    ):
        """Initialize network handler.
        
        Args:
            max_retries: Maximum number of retry attempts
            backoff_factor: Backoff factor for exponential retry delay
            retry_status_codes: HTTP status codes that should trigger retries
            connection_timeout: Connection timeout in seconds
            read_timeout: Read timeout in seconds
            health_check_interval: Health check interval in seconds
        """
        self.max_retries = max_retries
        self.backoff_factor = backoff_factor
        self.retry_status_codes = retry_status_codes
        self.connection_timeout = connection_timeout
        self.read_timeout = read_timeout
        self.health_check_interval = health_check_interval
        
        self._connection_failures = 0
        self._last_successful_request = datetime.now()
        self._is_connected = True
        self._health_check_thread = None
        self._stop_health_check = threading.Event()
        
        # Configure logging
        self.logger = logging.getLogger('NetworkHandler')
        if not self.logger.handlers:
            handler = logging.StreamHandler()
            formatter = logging.Formatter(
                '%(asctime)s - %(name)s - %(levelname)s - %(message)s'
            )
            handler.setFormatter(formatter)
            self.logger.addHandler(handler)
            self.logger.setLevel(logging.INFO)
    
    def create_session(self) -> requests.Session:
        """Create a requests session with retry strategy."""
        session = requests.Session()
        
        # Configure retry strategy
        retry_strategy = Retry(
            total=self.max_retries,
            backoff_factor=self.backoff_factor,
            status_forcelist=self.retry_status_codes,
            method_whitelist=["GET", "POST"]
        )
        
        # Mount adapter with retry strategy
        adapter = HTTPAdapter(max_retries=retry_strategy)
        session.mount("http://", adapter)
        session.mount("https://", adapter)
        
        # Set timeouts
        session.timeout = (self.connection_timeout, self.read_timeout)
        
        return session
    
    def make_request(
        self,
        session: requests.Session,
        method: str,
        url: str,
        **kwargs
    ) -> requests.Response:
        """Make HTTP request with error handling and logging.
        
        Args:
            session: Requests session
            method: HTTP method (GET, POST, etc.)
            url: Request URL
            **kwargs: Additional request parameters
            
        Returns:
            Response object
            
        Raises:
            requests.RequestException: If request fails after all retries
        """
        start_time = time.time()
        
        try:
            response = session.request(method, url, **kwargs)
            
            # Log successful request
            duration = time.time() - start_time
            self.logger.debug(f"Request to {url} completed in {duration:.2f}s")
            
            # Update connection status
            self._last_successful_request = datetime.now()
            if not self._is_connected:
                self.logger.info("Connection restored!")
                self._is_connected = True
                self._connection_failures = 0
            
            return response
            
        except requests.exceptions.ConnectionError as e:
            self._handle_connection_error(e, url)
            raise
        except requests.exceptions.Timeout as e:
            self._handle_timeout_error(e, url)
            raise
        except requests.exceptions.HTTPError as e:
            self._handle_http_error(e, url)
            raise
        except Exception as e:
            self.logger.error(f"Unexpected error during request to {url}: {e}")
            raise
    
    def _handle_connection_error(self, error: Exception, url: str):
        """Handle connection errors."""
        self._connection_failures += 1
        self._is_connected = False
        
        self.logger.warning(
            f"Connection error #{self._connection_failures} to {url}: {error}"
        )
        
        if self._connection_failures >= self.max_retries:
            self.logger.error(
                f"Max connection failures reached ({self.max_retries}). "
                f"Network may be down."
            )
    
    def _handle_timeout_error(self, error: Exception, url: str):
        """Handle timeout errors."""
        self.logger.warning(f"Timeout error for {url}: {error}")
    
    def _handle_http_error(self, error: Exception, url: str):
        """Handle HTTP errors."""
        self.logger.warning(f"HTTP error for {url}: {error}")
    
    def start_health_check(self, health_check_url: str = "https://api.vk.com"):
        """Start background health check monitoring.
        
        Args:
            health_check_url: URL to use for health checks
        """
        if self._health_check_thread and self._health_check_thread.is_alive():
            return
        
        self._stop_health_check.clear()
        self._health_check_thread = threading.Thread(
            target=self._health_check_worker,
            args=(health_check_url,),
            daemon=True
        )
        self._health_check_thread.start()
        self.logger.info(f"Started health check monitoring (interval: {self.health_check_interval}s)")
    
    def stop_health_check(self):
        """Stop background health check monitoring."""
        if self._health_check_thread:
            self._stop_health_check.set()
            self._health_check_thread.join(timeout=5)
            self.logger.info("Stopped health check monitoring")
    
    def _health_check_worker(self, health_check_url: str):
        """Background worker for health check monitoring."""
        session = self.create_session()
        
        while not self._stop_health_check.wait(self.health_check_interval):
            try:
                response = session.get(health_check_url, timeout=5)
                if response.status_code == 200:
                    if not self._is_connected:
                        self.logger.info("Health check: Connection restored")
                        self._is_connected = True
                        self._connection_failures = 0
                else:
                    self.logger.warning(f"Health check failed with status: {response.status_code}")
                    
            except Exception as e:
                if self._is_connected:
                    self.logger.warning(f"Health check failed: {e}")
                    self._is_connected = False
    
    def is_connected(self) -> bool:
        """Check if connection is healthy."""
        return self._is_connected
    
    def get_connection_stats(self) -> Dict[str, Any]:
        """Get connection statistics."""
        return {
            'is_connected': self._is_connected,
            'connection_failures': self._connection_failures,
            'last_successful_request': self._last_successful_request.isoformat(),
            'time_since_last_success': (datetime.now() - self._last_successful_request).total_seconds()
        }


class VkNetworkMixin:
    """Mixin to add network resilience to VK Bot class."""
    
    def __init__(self, *args, **kwargs):
        # Initialize network handler
        self.network_handler = NetworkHandler()
        self._vk_session = self.network_handler.create_session()
        
        super().__init__(*args, **kwargs)
        
        # Start health monitoring
        self.network_handler.start_health_check()
    
    def call_method(self, method: str, params: Optional[Dict[str, Any]] = None):
        """Override call_method to use network handler."""
        if params is None:
            params = {}
        
        # Add token and version to params
        params.update({
            'access_token': self.token,
            'v': self.api_version if hasattr(self, 'api_version') else '5.131'
        })
        
        url = f"https://api.vk.com/method/{method}"
        
        try:
            response = self.network_handler.make_request(
                self._vk_session, 'POST', url, data=params
            )
            response.raise_for_status()
            
            result = response.json()
            
            # Handle VK API errors
            if 'error' in result:
                error_code = result['error'].get('error_code', 0)
                error_msg = result['error'].get('error_msg', 'Unknown error')
                
                # Log VK API errors
                self.network_handler.logger.warning(
                    f"VK API error {error_code}: {error_msg} for method {method}"
                )
                
                # Handle specific error codes that might indicate network issues
                if error_code in [1, 6, 10]:  # Various server errors
                    time.sleep(1)  # Brief delay before allowing retry
                    
            return result
            
        except requests.exceptions.RequestException as e:
            self.network_handler.logger.error(
                f"Network error calling VK API method {method}: {e}"
            )
            raise
    
    def __del__(self):
        """Cleanup network handler on destruction."""
        if hasattr(self, 'network_handler'):
            self.network_handler.stop_health_check()