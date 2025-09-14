# -*- coding: utf-8 -*-
from typing import NoReturn
from unittest import (
    TestCase, defaultTestLoader,
    main as unitmain
)

from modules import (
    BetterBotBaseDataService, DataBuilder,
    VkInstance, Commands
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


class Test4LocalKarma(TestCase):
    """TestCase for local karma functionality
    """
    db = BetterBotBaseDataService('test_local_karma_db')
    
    @ordered
    def test_local_karma_data_structure(self) -> NoReturn:
        """Test local karma data storage and retrieval"""
        user = self.db.get_or_create_user(1, None)
        chat_id = 2000000001
        
        # Initially, local karma should be 0
        assert self.db.get_local_karma(user, chat_id) == 0
        
        # Set local karma
        self.db.set_local_karma(user, chat_id, 5)
        assert self.db.get_local_karma(user, chat_id) == 5
        
        # Test different chat has different karma
        chat_id_2 = 2000000002
        assert self.db.get_local_karma(user, chat_id_2) == 0
        
        self.db.set_local_karma(user, chat_id_2, 10)
        assert self.db.get_local_karma(user, chat_id) == 5  # First chat unchanged
        assert self.db.get_local_karma(user, chat_id_2) == 10  # Second chat

    @ordered  
    def test_build_local_karma(self) -> NoReturn:
        """Test local karma string building"""
        user = self.db.get_user(1)
        chat_id = 2000000001
        
        # Test with zero karma
        karma_str = DataBuilder.build_local_karma(user, self.db, chat_id)
        assert karma_str == "[5]"
        
        # Test with different karma value
        self.db.set_local_karma(user, chat_id, 15)
        karma_str = DataBuilder.build_local_karma(user, self.db, chat_id)
        assert karma_str == "[15]"

    @ordered
    def test_local_karma_commands(self) -> NoReturn:
        """Test local karma command functionality"""
        commands = Commands(VkInstance(), self.db)
        commands.peer_id = 2_000_000_001
        commands.karma_enabled = True
        commands.current_user = self.db.get_user(1)
        commands.user = self.db.get_user(1)
        
        # Test local karma message
        commands.local_karma_message()
        
        # Test local karma application (simulate)
        initial_karma = self.db.get_local_karma(commands.user, commands.peer_id)
        result = commands.apply_user_local_karma(commands.user, 3)
        assert result[2] == initial_karma  # initial karma
        assert result[3] == initial_karma + 3  # new karma
        
        # Verify karma was actually changed
        assert self.db.get_local_karma(commands.user, commands.peer_id) == initial_karma + 3

    @ordered
    def test_local_karma_sorting(self) -> NoReturn:
        """Test local karma user sorting"""
        # Create multiple users with different local karma
        user1 = self.db.get_or_create_user(10, None)
        user2 = self.db.get_or_create_user(20, None) 
        user3 = self.db.get_or_create_user(30, None)
        
        chat_id = 2000000001
        
        self.db.set_local_karma(user1, chat_id, 10)
        self.db.set_local_karma(user2, chat_id, 5)
        self.db.set_local_karma(user3, chat_id, 15)
        
        # Mock VK instance for testing
        class MockVk:
            def get_members_ids(self, peer_id):
                return [10, 20, 30]
        
        users = DataBuilder.get_users_sorted_by_local_karma(MockVk(), self.db, chat_id, chat_id)
        
        # Should be sorted by local karma descending
        assert users[0]["local_karma"] == 15  # user3
        assert users[1]["local_karma"] == 10  # user1  
        assert users[2]["local_karma"] == 5   # user2


if __name__ == '__main__':
    db = BetterBotBaseDataService("test_db")
    defaultTestLoader.sortTestMethodsUsing = compare
    unitmain(verbosity=2)
