# -*- coding: utf-8 -*-
from typing import (
    Optional, List, Dict, Any,
    NoReturn, Union, Callable
)

from social_ethosa import BetterBotBase, BetterUser
from saya import Vk


class BetterBotBaseDataService:
    """Class for interacting with the database.
    """
    def __init__(self, db_name: str = "users"):
        self.base = BetterBotBase(db_name, "dat")
        self.base.addPattern("programming_languages", [])
        self.base.addPattern("last_collective_vote", 0)
        self.base.addPattern("github_profile", "")
        self.base.addPattern("supporters", [])
        self.base.addPattern("opponents", [])
        self.base.addPattern("karma", 0)

    def get_or_create_user(
        self,
        uid: int,
        vk: Optional[Vk] = None
    ) -> BetterUser:
        """Returns a user object. Automatically creates it, if need.

        :param uid: user ID
        :param vk: Vk instance
        """
        if self.base.notInBD(uid):
            if vk:
                try:
                    response = vk.users.get(user_ids=uid)
                    if response and 'response' in response:
                        name = response['response'][0]["first_name"]
                    else:
                        name = "Пользователь"
                except Exception:
                    name = "Пользователь"
            else:
                name = "Пользователь"
            return self.base.addNew(uid=uid, name=name)
        return self.base.load(uid)

    def get_user_sorted_programming_languages(
        self,
        user: BetterUser
    ) -> List[str]:
        """Returns sorted list of user's programming languages.
        """
        languages = user.get("programming_languages", [])
        return sorted(set(languages))