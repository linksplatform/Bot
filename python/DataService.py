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
        if isinstance(user, dict):
            languages = user["programming_languages"] if "programming_languages" in user else []
        else:
            languages = user.programming_languages
        languages.sort()
        return languages

    def get_default_programming_language(self, language):
        for default_programming_language in config.default_programming_languages:
            default_programming_language = default_programming_language.replace('\\', '')
            if default_programming_language.lower() == language.lower():
                return default_programming_language
        return None

    def get_github_profile(self, user):
        if isinstance(user, dict):
            profile = user["github_profile"] if "github_profile" in user else ""
        else:
            profile = user.github_profile
        return profile

    def get_sorted_by_karma(self, other_keys, real_karma):
        users = self.getByKeys("karma", "name", *other_keys)
        sorted_users = sorted(
            users,
            key=real_karma,
            reverse=True
        )
        return sorted_users

    def get_by_name(self, other_keys):
        users = self.getByKeys("name", *other_keys)
        return users

    def apply_user_karma(self, user, amount):
        user.karma += amount
        return (user.uid, user.name, user.karma - amount, user.karma)

    def update_user_name(self, user, name):
        user.name = name
        self.base.save(user)

    def last_collective_vote(self):
        pass