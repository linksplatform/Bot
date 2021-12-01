# -*- coding: utf-8 -*-
from social_ethosa import BetterBotBase


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

    def get_or_create_user(self, user_id, vk):
        """
        Returns a user object. Automatically creates it, if need.
        """
        return self.base.autoInstall(user_id, vk)

    def get_user_sorted_programming_languages(self, user, sort=True, reverse_sort=False):
        """
        Returns user's programming languages.

        Arguments:
        - {user} -- user object;
        - {sort} -- return sorted list, if True;
        - {reverse_sort} -- uses for {sort} arg.
        """
        languages = BetterBotBaseDataService.get_user_property(user, "programming_languages")
        languages = languages if type(languages) == list else []
        if sort:
            return sorted(languages, reverse=reverse_sort)
        return languages

    def get_users(self, other_keys, sort_key=None, reverse_sort=True):
        """
        Returns users and their key values.

        Arguments:
        - {other_keys} -- list of user keys;
        - {sort_key} -- base key;
        """
        users = self.base.getByKeys(*other_keys)
        if sort_key:
            users = sorted(users, key=sort_key, reverse=reverse_sort)
        return users

    @staticmethod
    def get_user_property(user, property_name):
        return user[property_name] if isinstance(user, dict) else eval(f"user.{property_name}")

    @staticmethod
    def set_user_property(user, property_name, value):
        if isinstance(user, dict):
            user[property_name] = value
        else:
            exec(f"user.{property_name} = value")

    def save_user(self, user):
        self.base.save(user)
