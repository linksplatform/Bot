# -*- coding: utf-8 -*-
from typing import NoReturn
from unittest import (
    TestCase, defaultTestLoader,
    main as unitmain
)

from modules import (
    BetterBotBaseDataService, DataBuilder,
    VkInstance, Commands, QuestionsService
)
import patterns
import config


def make_orderer():
    order = {}

    def ordered(f):
        order[f.__name__] = len(order)
        return f

    def compare(a, b):
        return [1, -1][order[a] < order[b]]

    return ordered, compare
ordered, compare = make_orderer()


class Test1DataService(TestCase):
    """TestCase for commands.py
    """
    db = BetterBotBaseDataService('test_db')

    @ordered
    def test_get_or_create_user(
        self
    ) -> NoReturn:
        user_1 = db.get_or_create_user(1, None)
        user_2 = db.get_or_create_user(2, None)

        assert user_1.name == 'Пользователь'
        assert user_1.uid == 1

        user_1.programming_languages = ['C#', 'C++', 'Java', 'Python']
        user_1.github_profile = "Ethosa"
        user_2.programming_languages = []

        assert user_1.programming_languages == ['C#', 'C++', 'Java', 'Python']

        user_1.karma = 100
        user_2.karma = 9
        user_2.supporters = [1]
        db.save_user(user_1)
        db.save_user(user_2)

    @ordered
    def test_get_users(
        self
    ) -> NoReturn:
        users = db.get_users([], None)
        assert users == [{'uid': 1}, {'uid': 2}]

        # this must sorts as 100 -> 0
        users_sorted_by_karma = self.db.get_users(
            other_keys=["karma"],
            sort_key=lambda x: x["karma"]
            )
        assert users_sorted_by_karma[0] == {'karma': 100, 'uid': 1}

        # this must sorts as 0 -> 100
        users_sorted_by_karma_reversed = self.db.get_users(
            other_keys=["karma"],
            sort_key=lambda x: x["karma"],
            reverse_sort=False
            )
        assert users_sorted_by_karma_reversed[0] == {'karma': 9, 'uid': 2}


class Test2DataBuilder(TestCase):
    @ordered
    def test_build_programming_languages(
        self
    ) -> NoReturn:
        programming_languages = DataBuilder.build_programming_languages(
            db.get_user(1, None),
            db
        )
        assert programming_languages == 'C#, C++, Java, Python'

        programming_languages = DataBuilder.build_programming_languages(
            db.get_user(2, None), db, default='отсутствуют'
        )
        assert programming_languages == 'отсутствуют'

    @ordered
    def test_build_github_profile(
        self
    ) -> NoReturn:
        user = db.get_user(1, None)
        assert DataBuilder.build_github_profile(user, db) == f"github.com/{user.github_profile}"

    @ordered
    def test_build_karma(
        self
    ) -> NoReturn:
        user_1 = db.get_user(1)
        user_2 = db.get_user(2)

        print()
        print(DataBuilder.build_karma(user_1, db))
        print(DataBuilder.build_karma(user_2, db))


class Test3Commands(TestCase):
    commands = Commands(VkInstance(), BetterBotBaseDataService("test_db"))
    commands.peer_id = 2_000_000_001
    commands.karma_enabled = True

    @ordered
    def test_help_message(
        self
    ) -> NoReturn:
        self.commands.help_message()

    @ordered
    def test_info_message(
        self
    ) -> NoReturn:
        self.commands.current_user = db.get_user(2)
        self.commands.user = db.get_user(1)
        self.commands.info_message()

    @ordered
    def test_update_command(
        self
    ) -> NoReturn:
        self.commands.update_command()

    @ordered
    def test_change_programming_language(
        self
    ) -> NoReturn:
        self.commands.msg = '+= c#'
        self.commands.match_command(patterns.ADD_PROGRAMMING_LANGUAGE)
        self.commands.change_programming_language(True)

        self.commands.msg = '+= NeMeRlE'
        self.commands.match_command(patterns.ADD_PROGRAMMING_LANGUAGE)
        self.commands.change_programming_language(True)

        self.commands.msg = '-= C#'
        self.commands.match_command(patterns.REMOVE_PROGRAMMING_LANGUAGE)
        self.commands.change_programming_language(False)

    @ordered
    def test_change_github_profile(
        self
    ) -> NoReturn:
        self.commands.msg = '+= github.com/ethosa'
        self.commands.match_command(patterns.ADD_GITHUB_PROFILE)
        self.commands.change_github_profile(True)

        self.commands.msg = '-= github.com/ethosa'
        self.commands.match_command(patterns.REMOVE_GITHUB_PROFILE)
        self.commands.change_github_profile(False)

    @ordered
    def test_karma_message(
        self
    ) -> NoReturn:
        self.commands.karma_message()
        self.commands.user = db.get_user(2)
        self.commands.karma_message()

    @ordered
    def test_top(
        self
    ) -> NoReturn:
        self.commands.msg = 'top'
        self.commands.match_command(patterns.TOP)
        self.commands.top()
        self.commands.top(True)

    @ordered
    def test_top_lang(
        self
    ) -> NoReturn:
        self.commands.msg = 'top c#'
        self.commands.match_command(patterns.TOP_LANGUAGES)
        self.commands.top_langs()
        self.commands.top_langs(True)

        self.commands.msg = 'bottom c#'
        self.commands.match_command(patterns.BOTTOM_LANGUAGES)
        self.commands.top_langs()
        self.commands.top_langs(True)

    @ordered
    def test_apply_user_carma(
        self
    ) -> NoReturn:
        self.commands.user = db.get_user(1)
        self.commands.apply_user_karma(self.commands.user, 5)
        db.save_user(self.commands.user)
        self.commands.karma_message()

    @ordered
    def test_apply_collective_vote(
        self
    ) -> NoReturn:
        self.commands.current_user = db.get_user(2)
        self.commands.user = db.get_user(1)
        self.commands.apply_collective_vote("opponents", config.NEGATIVE_VOTES_PER_KARMA, -1)
        db.save_user(self.commands.user)
        self.commands.karma_message()

    @ordered
    def test_apply_karma_change(
        self
    ) -> NoReturn:
        self.commands.apply_karma_change('-', 6)
        self.commands.karma_message()


class Test3QuestionsService(TestCase):
    """TestCase for questions service functionality"""
    
    def setUp(self):
        self.questions_service = QuestionsService('test_questions.json', 'test_pinned.json')
        # Clear any existing data
        self.questions_service.questions = []
        self.questions_service.pinned_messages = {}
        self.questions_service.next_id = 1
    
    @ordered
    def test_add_question(self):
        """Test adding a question"""
        question_id = self.questions_service.add_question(
            question="How to use Python decorators?",
            user_id=123,
            user_name="TestUser",
            peer_id=2000000001,
            reward=5
        )
        
        self.assertEqual(question_id, 1)
        self.assertEqual(len(self.questions_service.questions), 1)
        
        question = self.questions_service.get_question_by_id(1)
        self.assertIsNotNone(question)
        self.assertEqual(question['question'], "How to use Python decorators?")
        self.assertEqual(question['user_id'], 123)
        self.assertEqual(question['reward'], 5)
        self.assertEqual(question['status'], 'open')
    
    @ordered
    def test_resolve_question(self):
        """Test resolving a question"""
        # Add a question first
        question_id = self.questions_service.add_question(
            question="Test question",
            user_id=123,
            user_name="TestUser",
            peer_id=2000000001,
            reward=10
        )
        
        # Resolve it
        resolved = self.questions_service.resolve_question(
            question_id=question_id,
            resolver_id=456,
            resolver_name="Resolver"
        )
        
        self.assertIsNotNone(resolved)
        self.assertEqual(resolved['status'], 'resolved')
        self.assertEqual(resolved['resolved_by'], 456)
        self.assertEqual(resolved['resolved_by_name'], "Resolver")
    
    @ordered
    def test_get_open_questions(self):
        """Test getting open questions sorted by reward"""
        # Add multiple questions
        self.questions_service.add_question("Question 1", 1, "User1", 2000000001, 5)
        self.questions_service.add_question("Question 2", 2, "User2", 2000000001, 15)
        self.questions_service.add_question("Question 3", 3, "User3", 2000000001, 10)
        
        open_questions = self.questions_service.get_open_questions(2000000001)
        
        # Should be sorted by reward descending
        self.assertEqual(len(open_questions), 3)
        self.assertEqual(open_questions[0]['reward'], 15)
        self.assertEqual(open_questions[1]['reward'], 10)
        self.assertEqual(open_questions[2]['reward'], 5)
    
    @ordered
    def test_pinned_messages(self):
        """Test pinned message management"""
        peer_id = 2000000001
        message_id = 12345
        
        # Set pinned message
        self.questions_service.set_pinned_message(peer_id, message_id)
        
        # Get pinned message
        pinned = self.questions_service.get_pinned_message(peer_id)
        self.assertEqual(pinned, message_id)
        
        # Clear pinned message
        self.questions_service.clear_pinned_message(peer_id)
        pinned = self.questions_service.get_pinned_message(peer_id)
        self.assertIsNone(pinned)
    
    @ordered
    def test_generate_desk_message(self):
        """Test generating questions desk message"""
        peer_id = 2000000001
        
        # Test empty desk
        message = self.questions_service.generate_questions_desk_message(peer_id)
        self.assertIn("пока нет вопросов", message)
        
        # Add questions
        self.questions_service.add_question("Question 1", 1, "User1", peer_id, 5)
        self.questions_service.add_question("Question 2", 2, "User2", peer_id, 0)
        
        message = self.questions_service.generate_questions_desk_message(peer_id)
        self.assertIn("📋 Доска вопросов", message)
        self.assertIn("Question 1", message)
        self.assertIn("Question 2", message)
        self.assertIn("🏆5", message)  # Reward display


class Test4QuestionsCommands(TestCase):
    """TestCase for questions commands functionality"""
    
    def setUp(self):
        self.vk = VkInstance()
        self.db = BetterBotBaseDataService('test_commands_db')
        self.commands = Commands(self.vk, self.db)
        self.commands.questions_service = QuestionsService('test_cmd_questions.json', 'test_cmd_pinned.json')
        self.commands.questions_service.questions = []
        self.commands.questions_service.pinned_messages = {}
        self.commands.questions_service.next_id = 1
        
        # Setup test user
        self.commands.current_user = self.db.get_or_create_user(123)
        self.commands.current_user.karma = 50
        self.commands.peer_id = 2000000001
        self.commands.karma_enabled = True
    
    @ordered
    def test_ask_question_command(self):
        """Test ask question command"""
        self.commands.msg = "ask How to learn Python? 10"
        self.commands.match_command(patterns.ASK_QUESTION)
        
        initial_karma = self.commands.current_user.karma
        self.commands.ask_question()
        
        # Check karma was deducted
        self.assertEqual(self.commands.current_user.karma, initial_karma - 10)
        
        # Check question was added
        questions = self.commands.questions_service.get_open_questions(self.commands.peer_id)
        self.assertEqual(len(questions), 1)
        self.assertEqual(questions[0]['question'], "How to learn Python?")
        self.assertEqual(questions[0]['reward'], 10)
    
    @ordered
    def test_resolve_question_command(self):
        """Test resolve question command"""
        # Add a question first
        question_id = self.commands.questions_service.add_question(
            question="Test question",
            user_id=456,
            user_name="Other User",
            peer_id=self.commands.peer_id,
            reward=15
        )
        
        self.commands.msg = f"resolve {question_id}"
        self.commands.match_command(patterns.RESOLVE_QUESTION)
        
        initial_karma = self.commands.current_user.karma
        self.commands.resolve_question()
        
        # Check karma was awarded
        self.assertEqual(self.commands.current_user.karma, initial_karma + 15)
        
        # Check question was resolved
        question = self.commands.questions_service.get_question_by_id(question_id)
        self.assertEqual(question['status'], 'resolved')
        self.assertEqual(question['resolved_by'], self.commands.current_user.uid)


if __name__ == '__main__':
    db = BetterBotBaseDataService("test_db")
    defaultTestLoader.sortTestMethodsUsing = compare
    unitmain(verbosity=2)
