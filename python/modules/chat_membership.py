# -*- coding: utf-8 -*-
"""Module for managing karma-based chat membership."""
from typing import Dict, List, Any, Optional, NoReturn
import logging

from saya import Vk
from social_ethosa import BetterUser

import config

# Set up logging
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)


class KarmaChatManager:
    """Manages automatic chat membership based on user karma levels."""
    
    def __init__(self, vk_instance: Vk):
        """Initialize the karma chat manager.
        
        :param vk_instance: VK API instance for making calls
        """
        self.vk = vk_instance
        self.karma_chats = config.KARMA_BASED_CHATS
        
    def check_and_update_membership(self, user: BetterUser) -> NoReturn:
        """Check user's karma and update their membership in all karma-based chats.
        
        :param user: User object to check
        """
        user_karma = user.karma
        user_id = user.uid
        
        logger.info(f"Checking membership for user {user_id} with karma {user_karma}")
        
        for chat_config in self.karma_chats:
            chat_id = chat_config["chat_id"]
            threshold = chat_config["karma_threshold"]
            chat_name = chat_config.get("name", f"Chat {chat_id}")
            
            # Check if user should be in this chat
            should_be_member = user_karma >= threshold
            
            # Check if user is currently a member
            is_currently_member = self._is_user_in_chat(user_id, chat_id)
            
            if should_be_member and not is_currently_member:
                # User should be added to chat
                self._add_user_to_chat(user_id, chat_id, chat_name, user_karma, threshold)
            elif not should_be_member and is_currently_member:
                # User should be removed from chat
                self._remove_user_from_chat(user_id, chat_id, chat_name, user_karma, threshold)
            else:
                # No change needed
                status = "member" if is_currently_member else "not member"
                logger.debug(f"User {user_id} is already correctly {status} of {chat_name}")
    
    def _is_user_in_chat(self, user_id: int, chat_id: int) -> bool:
        """Check if user is currently a member of the specified chat.
        
        :param user_id: User ID to check
        :param chat_id: Chat ID to check membership in
        :return: True if user is a member, False otherwise
        """
        try:
            members = self.vk.get_members_ids(chat_id)
            if members is None:
                logger.warning(f"Could not get member list for chat {chat_id}")
                return False
            return user_id in members
        except Exception as e:
            logger.error(f"Error checking membership for user {user_id} in chat {chat_id}: {e}")
            return False
    
    def _add_user_to_chat(
        self, 
        user_id: int, 
        chat_id: int, 
        chat_name: str, 
        user_karma: int, 
        threshold: int
    ) -> NoReturn:
        """Add user to chat.
        
        :param user_id: User ID to add
        :param chat_id: Chat ID to add user to
        :param chat_name: Human-readable chat name for logging
        :param user_karma: Current user karma
        :param threshold: Required karma threshold
        """
        try:
            # Convert chat_id to the format expected by VK API
            # VK chat IDs are typically in format 2000000000 + internal_chat_id
            internal_chat_id = chat_id - 2000000000
            
            result = self.vk.call_method(
                'messages.addChatUser',
                {
                    'chat_id': internal_chat_id,
                    'user_id': user_id
                }
            )
            
            if 'error' in result:
                error_code = result['error'].get('error_code', 'unknown')
                error_msg = result['error'].get('error_msg', 'Unknown error')
                logger.error(f"Failed to add user {user_id} to {chat_name}: {error_code} - {error_msg}")
            else:
                logger.info(f"Added user {user_id} to {chat_name} (karma: {user_karma} >= {threshold})")
                
        except Exception as e:
            logger.error(f"Exception adding user {user_id} to {chat_name}: {e}")
    
    def _remove_user_from_chat(
        self, 
        user_id: int, 
        chat_id: int, 
        chat_name: str, 
        user_karma: int, 
        threshold: int
    ) -> NoReturn:
        """Remove user from chat.
        
        :param user_id: User ID to remove
        :param chat_id: Chat ID to remove user from
        :param chat_name: Human-readable chat name for logging
        :param user_karma: Current user karma
        :param threshold: Required karma threshold
        """
        try:
            # Convert chat_id to the format expected by VK API
            internal_chat_id = chat_id - 2000000000
            
            result = self.vk.call_method(
                'messages.removeChatUser',
                {
                    'chat_id': internal_chat_id,
                    'member_id': user_id
                }
            )
            
            if 'error' in result:
                error_code = result['error'].get('error_code', 'unknown')
                error_msg = result['error'].get('error_msg', 'Unknown error')
                logger.error(f"Failed to remove user {user_id} from {chat_name}: {error_code} - {error_msg}")
            else:
                logger.info(f"Removed user {user_id} from {chat_name} (karma: {user_karma} < {threshold})")
                
        except Exception as e:
            logger.error(f"Exception removing user {user_id} from {chat_name}: {e}")
    
    def check_all_users_membership(self, data_service) -> NoReturn:
        """Check and update membership for all users in the database.
        
        This method can be used for periodic cleanup or initial setup.
        
        :param data_service: Data service instance to get all users
        """
        try:
            # Get all users with non-zero karma or programming languages
            users = data_service.get_users(['karma', 'programming_languages'], lambda x: x.get('karma', 0))
            
            logger.info(f"Checking membership for {len(users)} users")
            
            for user_data in users:
                user_id = user_data.get('uid')
                if user_id:
                    user = data_service.get_user(user_id)
                    self.check_and_update_membership(user)
                    
        except Exception as e:
            logger.error(f"Error during bulk membership check: {e}")
    
    def get_chat_members_by_karma(self, chat_id: int, data_service) -> Dict[str, List[Dict[str, Any]]]:
        """Get current members of a chat categorized by their karma eligibility.
        
        :param chat_id: Chat ID to analyze
        :param data_service: Data service instance
        :return: Dictionary with 'eligible' and 'ineligible' member lists
        """
        try:
            member_ids = self.vk.get_members_ids(chat_id)
            if not member_ids:
                return {'eligible': [], 'ineligible': []}
            
            # Find the karma threshold for this chat
            threshold = None
            for chat_config in self.karma_chats:
                if chat_config["chat_id"] == chat_id:
                    threshold = chat_config["karma_threshold"]
                    break
            
            if threshold is None:
                logger.warning(f"Chat {chat_id} is not configured for karma-based membership")
                return {'eligible': [], 'ineligible': []}
            
            eligible = []
            ineligible = []
            
            for member_id in member_ids:
                try:
                    user = data_service.get_user(member_id)
                    user_karma = user.karma
                    
                    member_info = {
                        'uid': member_id,
                        'name': user.name,
                        'karma': user_karma
                    }
                    
                    if user_karma >= threshold:
                        eligible.append(member_info)
                    else:
                        ineligible.append(member_info)
                        
                except Exception as e:
                    logger.error(f"Error getting user data for member {member_id}: {e}")
            
            return {'eligible': eligible, 'ineligible': ineligible}
            
        except Exception as e:
            logger.error(f"Error analyzing chat {chat_id}: {e}")
            return {'eligible': [], 'ineligible': []}