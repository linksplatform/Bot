# -*- coding: utf-8 -*-
"""Questions service for managing the questions desk."""
import json
import os
from typing import List, Dict, Any, Optional
from datetime import datetime


class QuestionsService:
    """Service for managing questions desk functionality."""
    
    def __init__(self, data_file: str = "questions.json", pinned_file: str = "pinned_messages.json"):
        self.data_file = data_file
        self.pinned_file = pinned_file
        self.questions = self._load_questions()
        self.pinned_messages = self._load_pinned_messages()
        self.next_id = self._get_next_id()
        
    def _load_questions(self) -> List[Dict[str, Any]]:
        """Load questions from file."""
        if os.path.exists(self.data_file):
            try:
                with open(self.data_file, 'r', encoding='utf-8') as f:
                    return json.load(f)
            except (json.JSONDecodeError, FileNotFoundError):
                return []
        return []
    
    def _save_questions(self) -> None:
        """Save questions to file."""
        with open(self.data_file, 'w', encoding='utf-8') as f:
            json.dump(self.questions, f, ensure_ascii=False, indent=2)
    
    def _load_pinned_messages(self) -> Dict[str, int]:
        """Load pinned message IDs from file."""
        if os.path.exists(self.pinned_file):
            try:
                with open(self.pinned_file, 'r', encoding='utf-8') as f:
                    return json.load(f)
            except (json.JSONDecodeError, FileNotFoundError):
                return {}
        return {}
    
    def _save_pinned_messages(self) -> None:
        """Save pinned message IDs to file."""
        with open(self.pinned_file, 'w', encoding='utf-8') as f:
            json.dump(self.pinned_messages, f, ensure_ascii=False, indent=2)
    
    def _get_next_id(self) -> int:
        """Get next available question ID."""
        if not self.questions:
            return 1
        return max(q.get('id', 0) for q in self.questions) + 1
    
    def add_question(
        self, 
        question: str, 
        user_id: int, 
        user_name: str,
        peer_id: int,
        reward: int = 0
    ) -> int:
        """Add a new question to the desk.
        
        Args:
            question: The question text
            user_id: ID of the user asking
            user_name: Name of the user asking
            peer_id: Chat ID where question was asked
            reward: Karma reward for solving this question
            
        Returns:
            ID of the created question
        """
        question_data = {
            'id': self.next_id,
            'question': question,
            'user_id': user_id,
            'user_name': user_name,
            'peer_id': peer_id,
            'reward': reward,
            'status': 'open',
            'created_at': datetime.now().isoformat(),
            'resolved_at': None,
            'resolved_by': None,
            'resolved_by_name': None
        }
        
        self.questions.append(question_data)
        self.next_id += 1
        self._save_questions()
        return question_data['id']
    
    def resolve_question(
        self, 
        question_id: int, 
        resolver_id: int, 
        resolver_name: str
    ) -> Optional[Dict[str, Any]]:
        """Mark a question as resolved.
        
        Args:
            question_id: ID of the question to resolve
            resolver_id: ID of the user resolving
            resolver_name: Name of the user resolving
            
        Returns:
            The resolved question data or None if not found
        """
        for question in self.questions:
            if question['id'] == question_id and question['status'] == 'open':
                question['status'] = 'resolved'
                question['resolved_at'] = datetime.now().isoformat()
                question['resolved_by'] = resolver_id
                question['resolved_by_name'] = resolver_name
                self._save_questions()
                return question
        return None
    
    def get_open_questions(self, peer_id: Optional[int] = None) -> List[Dict[str, Any]]:
        """Get all open questions, optionally filtered by chat.
        
        Args:
            peer_id: Optional chat ID to filter by
            
        Returns:
            List of open questions sorted by reward (descending)
        """
        open_questions = [
            q for q in self.questions 
            if q['status'] == 'open' and (peer_id is None or q['peer_id'] == peer_id)
        ]
        return sorted(open_questions, key=lambda x: x['reward'], reverse=True)
    
    def get_question_by_id(self, question_id: int) -> Optional[Dict[str, Any]]:
        """Get a specific question by ID."""
        for question in self.questions:
            if question['id'] == question_id:
                return question
        return None
    
    def get_user_questions(self, user_id: int) -> List[Dict[str, Any]]:
        """Get all questions asked by a specific user."""
        return [q for q in self.questions if q['user_id'] == user_id]
    
    def get_user_resolved_questions(self, user_id: int) -> List[Dict[str, Any]]:
        """Get all questions resolved by a specific user."""
        return [q for q in self.questions if q['resolved_by'] == user_id]
    
    def set_pinned_message(self, peer_id: int, message_id: int) -> None:
        """Set the pinned message ID for a chat."""
        self.pinned_messages[str(peer_id)] = message_id
        self._save_pinned_messages()
    
    def get_pinned_message(self, peer_id: int) -> Optional[int]:
        """Get the pinned message ID for a chat."""
        return self.pinned_messages.get(str(peer_id))
    
    def clear_pinned_message(self, peer_id: int) -> None:
        """Clear the pinned message ID for a chat."""
        if str(peer_id) in self.pinned_messages:
            del self.pinned_messages[str(peer_id)]
            self._save_pinned_messages()
    
    def generate_questions_desk_message(self, peer_id: int) -> str:
        """Generate the questions desk message for pinning."""
        open_questions = self.get_open_questions(peer_id)
        
        if not open_questions:
            return (
                "📋 Доска вопросов\n\n"
                "Здесь пока нет вопросов.\n\n"
                "Используйте 'ask [вопрос] [награда]' чтобы добавить вопрос."
            )
        
        message = "📋 Доска вопросов\n\n"
        
        for i, question in enumerate(open_questions[:10], 1):  # Show top 10
            reward_text = f" 🏆{question['reward']}" if question['reward'] > 0 else ""
            # Truncate long questions for the pinned message
            question_text = question['question']
            if len(question_text) > 80:
                question_text = question_text[:77] + "..."
            
            message += f"{i}. #{question['id']}: {question_text}{reward_text}\n"
        
        if len(open_questions) > 10:
            message += f"\n... и еще {len(open_questions) - 10} вопросов"
        
        message += "\n\nИспользуйте 'resolve [ID]' чтобы решить вопрос."
        message += "\nИспользуйте 'questions' чтобы увидеть полный список."
        
        return message