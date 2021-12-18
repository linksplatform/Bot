# -*- coding: utf-8 -*-
from typing import Optional, List, Dict, Any, NoReturn, Union

from social_ethosa import BetterBotBase, BetterUser
from saya import Vk


class BetterBotBaseDataService:
    """
    Class for interacting with the database.
    """
    def __init__(self):
        self.base = BetterBotBase("users", "dat")
        self.base.addPattern("programming_languages", [])
        self.base.addPattern("last_collective_vote", 0)
        self.base.addPattern("github_profile", "")
        self.base.addPattern("supporters", [])
        self.base.addPattern("opponents", [])
        self.base.addPattern("karma", 0)

    def get_or_create_user(
        self,
        user_id: int,
        vk: Vk
    ):
        """Returns a user object. Automatically creates it, if need.
        """
        return self.base.autoInstall(user_id, vk)

    def get_users(
        self,
        other_keys: List[str],
        sort_key: Optional[str],
        reverse_sort: bool = True
    ) -> List[BetterUser]:
        """Returns users and their key values.

        Arguments:
        - {other_keys} -- list of user keys;
        - {sort_key} -- base key;
        """
        users = self.base.getByKeys(*other_keys)
        if sort_key:
            users = sorted(users, key=sort_key, reverse=reverse_sort)
        return users

    @staticmethod
    def get_user_sorted_programming_languages(
        user,
        sort: bool = True,
        reverse_sort: bool = False
    ) -> List[str]:
        """Returns user's programming languages.

        :param user: -- user object;
        :param sort: -- return sorted list, if True;
        :param reverse_sort: -- uses for {sort} arg.
        """
        languages = BetterBotBaseDataService.get_user_property(user, "programming_languages")
        languages = languages if isinstance(languages, list) else []
        if sort:
            return sorted(languages, reverse=reverse_sort)
        return languages

    @staticmethod
    def get_user_property(
        user: Union[Dict[str, Any], BetterUser],
        property_name: str
    ) -> Any:
        return user[property_name] if isinstance(user, dict) else eval(f"user.{property_name}")

    @staticmethod
    def set_user_property(
        user: Union[Dict[str, Any], BetterUser],
        property_name: str,
        value: Any
    ) -> NoReturn:
        if isinstance(user, dict):
            user[property_name] = value
        else:
            exec(f"user.{property_name} = value")

    def save_user(
        self,
        user: BetterUser
    ):
        self.base.save(user)
