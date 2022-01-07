# -*- coding: utf-8 -*-
from typing import NoReturn
from unittest import (
    TestCase, defaultTestLoader,
    main as unitmain
)

from modules import (
    BetterBotBaseDataService, DataBuilder
)


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
        user_1 = self.db.get_or_create_user(1, None)
        user_2 = self.db.get_or_create_user(2, None)

        assert user_1.name == 'Пользователь'
        assert user_1.uid == 1

        user_1.programming_languages = ['C#', 'C++', 'Java', 'Python']
        user_2.programming_languages = []

        assert user_1.programming_languages == ['C#', 'C++', 'Java', 'Python']

        user_1.karma = 100
        self.db.save_user(user_1)
        self.db.save_user(user_2)

    @ordered
    def test_get_users(
        self
    ) -> NoReturn:
        users = self.db.get_users([], None)
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
        assert users_sorted_by_karma_reversed[0] == {'karma': 0, 'uid': 2}


class Test2DataBuilder(TestCase):
    db = BetterBotBaseDataService('test_db')

    @ordered
    def test_build_programming_languages(
        self
    ) -> NoReturn:
        programming_languages = DataBuilder.build_programming_languages(
            self.db.get_user(1, None),
            self.db
        )
        assert programming_languages == 'C#, C++, Java, Python'

        programming_languages = DataBuilder.build_programming_languages(
            self.db.get_user(2, None),
            self.db,
            default='отсутствуют'
        )
        assert programming_languages == 'отсутствуют'




if __name__ == '__main__':
    defaultTestLoader.sortTestMethodsUsing = compare
    unitmain(verbosity=2)
