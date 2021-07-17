from social_ethosa import BetterBotBase
import config


class BetterBotBaseDataService(BetterBotBase):
    def __init__(self, *args):
        super().__init__(*args)
        self.base = BetterBotBase("users", "dat")
        self.base.addPattern("programming_languages", [])
        self.base.addPattern("last_collective_vote", 0)
        self.base.addPattern("github_profile", "")
        self.base.addPattern("supporters", [])
        self.base.addPattern("opponents", [])
        self.base.addPattern("karma", 0)

    def user_auto_install(self, from_id, vk):
        return self.base.autoInstall(from_id, vk) if from_id > 0 else None

    def get_programming_languages(self, user):
        languages = self.get_user_property(user, "programming_languages")
        languages.sort()
        return languages

    def get_user_sorted_programming_languages(self, user):
        if isinstance(user, dict):
            languages = user["programming_languages"] if "programming_languages" in user else []
        else:
            languages = user.programming_languages
        languages.sort()
        return languages

    def get_github_profile(self, user):
        profile = self.get_user_property(user, "github_profile")
        return profile

    def get_sorted_by_karma(self, other_keys, sort_key):
        users = self.getByKeys("karma", "name", *other_keys)
        sorted_users = sorted(
            users,
            key=sort_key,
            reverse=True
        )
        return sorted_users

    def get_by_name(self, other_keys):
        users = self.getByKeys("name", *other_keys)
        return users

    def get_user_property(self, user, property_name):
        return user[property_name] if isinstance(user, dict) else eval(f"user.{property_name}")

    def set_user_property(self, user, property_name, value):
        user[property_name] = value if isinstance(user, dict) else exec(f"user.{property_name} = value")
