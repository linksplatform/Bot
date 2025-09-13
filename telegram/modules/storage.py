# -*- coding: utf-8 -*-
"""Simple JSON-based storage for user data (no SQL required)."""

import json
import os
from datetime import datetime
from typing import Dict, List, Optional, Any
from dataclasses import dataclass, asdict
import config


@dataclass
class User:
    """User data structure."""
    user_id: int
    username: str = ""
    first_name: str = ""
    karma: int = 0
    programming_languages: List[str] = None
    github_profile: str = ""
    last_karma_vote: Optional[str] = None  # ISO format datetime
    
    def __post_init__(self):
        if self.programming_languages is None:
            self.programming_languages = []


@dataclass
class KarmaVote:
    """Karma vote structure."""
    voter_id: int
    target_id: int
    vote_type: str  # 'positive' or 'negative'
    timestamp: str  # ISO format datetime
    chat_id: int


class Storage:
    """Simple JSON-based storage manager."""
    
    def __init__(self):
        self.users: Dict[int, User] = {}
        self.karma_votes: List[KarmaVote] = []
        self._ensure_data_dir()
        self._load_data()
    
    def _ensure_data_dir(self):
        """Create data directory if it doesn't exist."""
        os.makedirs(config.DATA_DIR, exist_ok=True)
    
    def _load_data(self):
        """Load data from JSON files."""
        # Load users
        if os.path.exists(config.USERS_FILE):
            try:
                with open(config.USERS_FILE, 'r', encoding='utf-8') as f:
                    users_data = json.load(f)
                    for user_id_str, user_data in users_data.items():
                        user_id = int(user_id_str)
                        self.users[user_id] = User(**user_data)
            except (json.JSONDecodeError, TypeError, ValueError):
                pass  # Start with empty data if file is corrupted
        
        # Load karma votes
        if os.path.exists(config.KARMA_VOTES_FILE):
            try:
                with open(config.KARMA_VOTES_FILE, 'r', encoding='utf-8') as f:
                    votes_data = json.load(f)
                    self.karma_votes = [KarmaVote(**vote) for vote in votes_data]
            except (json.JSONDecodeError, TypeError, ValueError):
                pass  # Start with empty data if file is corrupted
    
    def _save_users(self):
        """Save users to JSON file."""
        users_data = {str(user_id): asdict(user) for user_id, user in self.users.items()}
        with open(config.USERS_FILE, 'w', encoding='utf-8') as f:
            json.dump(users_data, f, ensure_ascii=False, indent=2)
    
    def _save_karma_votes(self):
        """Save karma votes to JSON file."""
        votes_data = [asdict(vote) for vote in self.karma_votes]
        with open(config.KARMA_VOTES_FILE, 'w', encoding='utf-8') as f:
            json.dump(votes_data, f, ensure_ascii=False, indent=2)
    
    def get_user(self, user_id: int, username: str = "", first_name: str = "") -> User:
        """Get or create user."""
        if user_id not in self.users:
            self.users[user_id] = User(
                user_id=user_id,
                username=username,
                first_name=first_name
            )
            self._save_users()
        else:
            # Update user info if provided
            user = self.users[user_id]
            if username:
                user.username = username
            if first_name:
                user.first_name = first_name
            self._save_users()
        
        return self.users[user_id]
    
    def update_user(self, user: User):
        """Update user data."""
        self.users[user.user_id] = user
        self._save_users()
    
    def add_karma_vote(self, vote: KarmaVote):
        """Add a karma vote."""
        self.karma_votes.append(vote)
        self._save_karma_votes()
    
    def get_recent_karma_votes(self, target_id: int, chat_id: int, hours: int = 24) -> List[KarmaVote]:
        """Get recent karma votes for a user in a chat."""
        cutoff_time = datetime.now().timestamp() - (hours * 3600)
        recent_votes = []
        
        for vote in self.karma_votes:
            if (vote.target_id == target_id and 
                vote.chat_id == chat_id and
                datetime.fromisoformat(vote.timestamp).timestamp() > cutoff_time):
                recent_votes.append(vote)
        
        return recent_votes
    
    def get_chat_users(self, chat_id: int, member_ids: List[int]) -> List[User]:
        """Get users that are members of a specific chat."""
        chat_users = []
        for member_id in member_ids:
            if member_id in self.users:
                chat_users.append(self.users[member_id])
        return chat_users
    
    def cleanup_old_votes(self, days: int = 30):
        """Remove votes older than specified days."""
        cutoff_time = datetime.now().timestamp() - (days * 24 * 3600)
        self.karma_votes = [
            vote for vote in self.karma_votes
            if datetime.fromisoformat(vote.timestamp).timestamp() > cutoff_time
        ]
        self._save_karma_votes()


# Global storage instance
storage = Storage()