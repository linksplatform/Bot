# -*- coding: utf-8 -*-
from typing import NoReturn
import unittest

from modules import BetterBotBaseDataService


class DataServiceTest(unittest.TestCase):
    """TestCase for commands.py
    """
    db = BetterBotBaseDataService('test_db')

    def test_get_or_create_user(
        self
    ) -> NoReturn:
        user_1 = self.db.get_or_create_user(1, None)
        user_2 = self.db.get_or_create_user(2, None)

        assert user_1.name == 'Пользователь'
        assert user_1.uid == 1

        user_1.programming_languages = ['C#', 'C++', 'Java', 'Python']
        user_2.programming_languages = ['C#', 'C++', 'Java', 'Python']

        assert user_1.programming_languages == ['C#', 'C++', 'Java', 'Python']

        user_1.karma = 100
        self.db.save_user(user_1)
        self.db.save_user(user_2)

    def test_get_users(
        self
    ) -> NoReturn:
        users = self.db.get_users([], None)
        assert users == [{'uid': 1}, {'uid': 2}]

        # this sorts as 100 -> 0
        users_sorted_by_karma = self.db.get_users(
            other_keys=["karma"],
            sort_key=lambda x: x["karma"]
            )
        assert users_sorted_by_karma[0] == {'karma': 100, 'uid': 1}

        # this sorts as 0 -> 100
        users_sorted_by_karma_reversed = self.db.get_users(
            other_keys=["karma"],
            sort_key=lambda x: x["karma"],
            reverse_sort=False
            )
        assert users_sorted_by_karma_reversed[0] == {'karma': 0, 'uid': 2}


if __name__ == '__main__':
    unittest.TestLoader.sortTestMethodsUsing = None
    unittest.main()
