from social_ethosa import BetterBotBase


class BetterBotBaseDataService:
    def __init__(self):
        self.base = BetterBotBase("users", "dat")
        self.base.addPattern("programming_languages", [])
        self.base.addPattern("last_collective_vote", 0)
        self.base.addPattern("github_profile", "")
        self.base.addPattern("supporters", [])
        self.base.addPattern("opponents", [])
        self.base.addPattern("karma", 0)

    def get_or_create_user(self, user_id, vk):
        return self.base.autoInstall(user_id, vk)

    def get_user_sorted_programming_languages(self, user):
        languages = self.get_user_property(user, "programming_languages")
        languages = languages if type(languages) == list else []
        languages.sort()
        return languages

    def get_users_sorted_by_karma(self, other_keys, sort_key=None):
        users = self.base.getByKeys("name", *other_keys)
        sorted_users = sorted(
            users,
            key=sort_key,
            reverse=True
        )
        return sorted_users

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
