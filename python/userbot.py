# -*- coding: utf-8 -*-
"""Provides working with VK API as user.
"""
from typing import NoReturn, List, Dict, Any
import time
import threading

from exceptions import TooManyMessagesError
from tokens import USER_TOKEN
from requests import Session


class UserBot:
    """Automatically deleting unnecessary messages and handling friend requests.
    """
    session = Session()
    url = 'https://api.vk.com/method/'
    token = USER_TOKEN
    
    def __init__(self):
        self._friend_request_checker_active = False
        self._friend_request_thread = None

    @staticmethod
    def delete_messages(
        conversation_message_ids: List[int],
        peer_id: int
    ) -> NoReturn:
        """Deletes all conversations messages
        """
        if len(conversation_message_ids) <= 24:
            params = {
                'conversation_message_ids': conversation_message_ids,
                'peer_id': peer_id
            }
            code = '''
            var ids = API.messages.getByConversationMessageId(%s).items@.id;
            var deleted = [];
            var index = 0;
            while (index < ids.length){
                API.messages.delete({"message_ids": ids[index], "delete_for_all": 1 });
                index = index + 1;
            }
            return 1;'''
            data = {
                'access_token': UserBot.token,
                'code': code % params,
                'v': '5.103'
            }
            return UserBot.execute(data)
        raise TooManyMessagesError(
            'Maximum amount was reached (%d/24)' % len(conversation_message_ids))

    @staticmethod
    def execute(data: str) -> Dict[str, Any]:
        """Executes VK Script.
        """
        return UserBot.session.post(UserBot.url + 'execute', data=data).json()
    
    @classmethod
    def call_method(cls, method: str, params: Dict[str, Any]) -> Dict[str, Any]:
        """Make VK API call with user token.
        """
        params['access_token'] = cls.token
        params['v'] = '5.131'
        response = cls.session.post(cls.url + method, data=params)
        return response.json()
    
    def get_friend_requests(self) -> List[int]:
        """Get list of pending friend request user IDs.
        """
        if not self.token:
            print("Warning: USER_TOKEN not set, cannot check friend requests")
            return []
            
        response = self.call_method('friends.getRequests', {
            'out': 0,  # incoming requests
            'count': 1000
        })
        
        if 'error' in response:
            print(f"Error getting friend requests: {response['error']}")
            return []
            
        if 'response' not in response:
            return []
            
        return response['response'].get('items', [])
    
    def accept_friend_request(self, user_id: int) -> bool:
        """Accept a friend request from user_id.
        
        Returns:
            bool: True if successfully accepted, False otherwise
        """
        if not self.token:
            print("Warning: USER_TOKEN not set, cannot accept friend requests")
            return False
            
        response = self.call_method('friends.add', {'user_id': user_id})
        
        if 'error' in response:
            error_code = response['error'].get('error_code', 0)
            if error_code == 174:
                print(f"Cannot add yourself as friend (user {user_id})")
            elif error_code == 175:
                print(f"User {user_id} has blocked you")
            elif error_code == 176:
                print(f"You have blocked user {user_id}")
            elif error_code == 177:
                print(f"User {user_id} not found")
            elif error_code == 242:
                print(f"Too many friends, cannot add user {user_id}")
            else:
                print(f"Error accepting friend request from {user_id}: {response['error']}")
            return False
        
        if 'response' in response:
            response_code = response['response']
            if response_code == 2:
                print(f"Successfully accepted friend request from user {user_id}")
                return True
            elif response_code == 1:
                print(f"Friend request sent to user {user_id} (was not pending)")
                return True
            elif response_code == 4:
                print(f"Request resent to user {user_id}")
                return True
        
        return False
    
    def check_and_accept_friend_requests(self):
        """Check for pending friend requests and accept them automatically.
        """
        try:
            pending_requests = self.get_friend_requests()
            
            if pending_requests:
                print(f"Found {len(pending_requests)} pending friend request(s)")
                
                for user_id in pending_requests:
                    print(f"Accepting friend request from user {user_id}")
                    success = self.accept_friend_request(user_id)
                    
                    if success:
                        # Small delay between requests to avoid rate limits
                        time.sleep(1)
                        
            elif pending_requests == []:
                pass  # No pending requests, no output needed
            
        except Exception as e:
            print(f"Error checking friend requests: {e}")
    
    def start_friend_request_monitor(self, check_interval: int = 60):
        """Start monitoring for friend requests in a background thread.
        
        Args:
            check_interval: Time in seconds between checks (default: 60)
        """
        if not self.token:
            print("Warning: USER_TOKEN not set, cannot start friend request monitoring")
            return
            
        if self._friend_request_checker_active:
            print("Friend request monitor is already running")
            return
        
        print(f"Starting friend request monitor (checking every {check_interval} seconds)")
        self._friend_request_checker_active = True
        
        def monitor_loop():
            while self._friend_request_checker_active:
                self.check_and_accept_friend_requests()
                time.sleep(check_interval)
        
        self._friend_request_thread = threading.Thread(target=monitor_loop, daemon=True)
        self._friend_request_thread.start()
    
    def stop_friend_request_monitor(self):
        """Stop the friend request monitoring thread.
        """
        if self._friend_request_checker_active:
            print("Stopping friend request monitor")
            self._friend_request_checker_active = False
            if self._friend_request_thread:
                self._friend_request_thread.join(timeout=5)
