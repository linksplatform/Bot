# -*- coding: utf-8 -*-
"""Daily outreach module for asking users about programming languages and GitHub profiles."""
import random
from datetime import datetime, timedelta
from typing import List, NoReturn, Optional
from regex import search, IGNORECASE

from .data_service import BetterBotBaseDataService
import sys
import os
sys.path.append(os.path.dirname(os.path.dirname(__file__)))
import config


class DailyOutreach:
    """Handles daily outreach to users without programming languages or GitHub profiles."""
    
    QUESTIONS = [
        "У вас есть страничка на GitHub?",
        "Какие языки программирования вы знаете?"
    ]
    
    def __init__(self, vk_instance, data_service: BetterBotBaseDataService):
        self.vk_instance = vk_instance
        self.data_service = data_service
        self.last_outreach_date = None
    
    def should_run_daily_outreach(self) -> bool:
        """Check if daily outreach should run (once per day)."""
        today = datetime.now().date()
        if self.last_outreach_date != today:
            self.last_outreach_date = today
            return True
        return False
    
    def find_users_without_profile_info(self, peer_id: int) -> List[int]:
        """Find users without programming languages or GitHub profiles."""
        try:
            member_ids = self.vk_instance.get_members_ids(peer_id)
            if not member_ids:
                return []
            
            candidates = []
            for uid in member_ids:
                user = self.data_service.get_user(uid, self.vk_instance)
                
                # Check if user has no programming languages and no GitHub profile
                has_languages = (hasattr(user, 'programming_languages') and 
                               user.programming_languages and 
                               len(user.programming_languages) > 0)
                has_github = (hasattr(user, 'github_profile') and 
                             user.github_profile and 
                             user.github_profile.strip() != "")
                
                if not has_languages and not has_github:
                    candidates.append(uid)
            
            return candidates
        except Exception as e:
            print(f"Error finding users: {e}")
            return []
    
    def select_random_user_and_question(self, candidates: List[int]) -> Optional[tuple]:
        """Select a random user and question."""
        if not candidates:
            return None
        
        user_id = random.choice(candidates)
        question = random.choice(self.QUESTIONS)
        return user_id, question
    
    def send_daily_question(self, peer_id: int) -> NoReturn:
        """Send daily question to a random user."""
        candidates = self.find_users_without_profile_info(peer_id)
        
        if not candidates:
            return
        
        selection = self.select_random_user_and_question(candidates)
        if not selection:
            return
        
        user_id, question = selection
        try:
            user_name = self.vk_instance.get_user_name(user_id, "nom")
            message = f"[id{user_id}|{user_name}], {question}"
            self.vk_instance.send_msg(message, peer_id)
        except Exception as e:
            print(f"Error sending daily question: {e}")
    
    def process_potential_response(self, msg: str, from_id: int) -> bool:
        """
        Process a message that might be a response to our daily questions.
        Returns True if response was processed, False otherwise.
        """
        msg_lower = msg.lower()
        
        # Check for GitHub profile response
        github_match = search(r'github\.com/([a-zA-Z0-9-_]+)', msg, IGNORECASE)
        if github_match:
            username = github_match.group(1)
            user = self.data_service.get_user(from_id, self.vk_instance)
            user.github_profile = username
            self.data_service.save_user(user)
            return True
        
        # Check for programming language response
        for lang in config.DEFAULT_PROGRAMMING_LANGUAGES:
            # Remove regex escaping for matching
            lang_clean = lang.replace(r'\+', '+').replace(r'\-', '-').replace(r'\\', '')
            if search(lang_clean, msg, IGNORECASE):
                user = self.data_service.get_user(from_id, self.vk_instance)
                if not hasattr(user, 'programming_languages'):
                    user.programming_languages = []
                if lang_clean not in user.programming_languages:
                    user.programming_languages.append(lang_clean)
                self.data_service.save_user(user)
                return True
        
        return False