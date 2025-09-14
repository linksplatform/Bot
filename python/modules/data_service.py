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
        self.base.addPattern("supporters", {})  # {chat_id: [user_ids]}
        self.base.addPattern("opponents", {})   # {chat_id: [user_ids]}
        self.base.addPattern("karma", {})       # {chat_id: karma_value}

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
                name = vk.users.get(user_ids=uid)['response'][0]["first_name"]
            else:
                name = "Пользователь"
            return self.base.addNew(uid=uid, name=name)
        return self.base.load(uid)

    def get_user(
        self,
        uid: int,
        vk: Optional[Vk] = None
    ) -> BetterUser:
        """Alias for get_or_create_user.
        """
        return self.get_or_create_user(uid, vk)

    def get_users(
        self,
        other_keys: List[str],
        sort_key: Optional[Callable[[Any], Any]],
        reverse_sort: bool = True
    ) -> List[Dict[str, Any]]:
        """Returns users and their key values.

        :param other_key: list of user keys
        :param sort_key: base key
        :param reverse_sort: if True returns reversed list.
        """
        users = self.base.getByKeys(*other_keys)
        if sort_key:
            users = sorted(users, key=sort_key, reverse=reverse_sort)
        return users

    @staticmethod
    def get_user_sorted_programming_languages(
        user: BetterUser,
        sort: bool = True,
        reverse_sort: bool = False
    ) -> List[str]:
        """Returns user's programming languages.

        :param user: -- user object
        :param sort: -- return sorted list, if True
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
        """
        :param user: dict or BetterUser
        :param property_name: needed property
        """
        return user[property_name] if isinstance(user, dict) else eval(f"user.{property_name}")

    @staticmethod
    def set_user_property(
        user: Union[Dict[str, Any], BetterUser],
        property_name: str,
        value: Any
    ) -> NoReturn:
        """Changes user property

        :param user: dict or BetterUser
        :param property_name: needed property
        :param value: new value
        """
        if isinstance(user, dict):
            user[property_name] = value
        else:
            exec(f"user.{property_name} = value")

    def save_user(
        self,
        user: BetterUser
    ) -> NoReturn:
        self.base.save(user)

    @staticmethod
    def get_user_chat_karma(
        user: Union[Dict[str, Any], BetterUser],
        chat_id: int
    ) -> int:
        """Get user's karma for a specific chat.
        
        :param user: dict or BetterUser
        :param chat_id: chat ID
        """
        karma_dict = BetterBotBaseDataService.get_user_property(user, "karma")
        if isinstance(karma_dict, dict):
            return karma_dict.get(chat_id, 0)
        # Backward compatibility: treat old integer karma as global
        return karma_dict if isinstance(karma_dict, int) else 0

    @staticmethod
    def set_user_chat_karma(
        user: Union[Dict[str, Any], BetterUser],
        chat_id: int,
        karma_value: int
    ) -> NoReturn:
        """Set user's karma for a specific chat.
        
        :param user: dict or BetterUser
        :param chat_id: chat ID
        :param karma_value: new karma value
        """
        karma_dict = BetterBotBaseDataService.get_user_property(user, "karma")
        if not isinstance(karma_dict, dict):
            # Migrate from old integer karma to dict
            karma_dict = {} if karma_dict == 0 else {chat_id: karma_dict}
        karma_dict[chat_id] = karma_value
        BetterBotBaseDataService.set_user_property(user, "karma", karma_dict)

    @staticmethod
    def get_user_chat_supporters(
        user: Union[Dict[str, Any], BetterUser],
        chat_id: int
    ) -> List[int]:
        """Get user's supporters for a specific chat.
        
        :param user: dict or BetterUser
        :param chat_id: chat ID
        """
        supporters_dict = BetterBotBaseDataService.get_user_property(user, "supporters")
        if isinstance(supporters_dict, dict):
            return supporters_dict.get(chat_id, [])
        # Backward compatibility: treat old list as global
        return supporters_dict if isinstance(supporters_dict, list) else []

    @staticmethod
    def set_user_chat_supporters(
        user: Union[Dict[str, Any], BetterUser],
        chat_id: int,
        supporters: List[int]
    ) -> NoReturn:
        """Set user's supporters for a specific chat.
        
        :param user: dict or BetterUser
        :param chat_id: chat ID
        :param supporters: list of supporter user IDs
        """
        supporters_dict = BetterBotBaseDataService.get_user_property(user, "supporters")
        if not isinstance(supporters_dict, dict):
            # Migrate from old list to dict
            supporters_dict = {} if not supporters_dict else {chat_id: supporters_dict}
        supporters_dict[chat_id] = supporters
        BetterBotBaseDataService.set_user_property(user, "supporters", supporters_dict)

    @staticmethod
    def get_user_chat_opponents(
        user: Union[Dict[str, Any], BetterUser],
        chat_id: int
    ) -> List[int]:
        """Get user's opponents for a specific chat.
        
        :param user: dict or BetterUser
        :param chat_id: chat ID
        """
        opponents_dict = BetterBotBaseDataService.get_user_property(user, "opponents")
        if isinstance(opponents_dict, dict):
            return opponents_dict.get(chat_id, [])
        # Backward compatibility: treat old list as global
        return opponents_dict if isinstance(opponents_dict, list) else []

    @staticmethod
    def set_user_chat_opponents(
        user: Union[Dict[str, Any], BetterUser],
        chat_id: int,
        opponents: List[int]
    ) -> NoReturn:
        """Set user's opponents for a specific chat.
        
        :param user: dict or BetterUser
        :param chat_id: chat ID
        :param opponents: list of opponent user IDs
        """
        opponents_dict = BetterBotBaseDataService.get_user_property(user, "opponents")
        if not isinstance(opponents_dict, dict):
            # Migrate from old list to dict
            opponents_dict = {} if not opponents_dict else {chat_id: opponents_dict}
        opponents_dict[chat_id] = opponents
        BetterBotBaseDataService.set_user_property(user, "opponents", opponents_dict)
