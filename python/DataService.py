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

    def get_programming_languages_string(self, user):
        if isinstance(user, dict):
            languages = user["programming_languages"] if "programming_languages" in user else []
        else:
            languages = user.programming_languages
        languages.sort()
        return ", ".join(languages) if len(languages) > 0 else "отсутствуют"

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
        return f"github.com/{profile}" if profile else ""

    def get_github_profile_top_string(self, user):
        profile = self.get_github_profile(user)
        if profile:
            profile = f" — {profile}"
        return profile

    def get_programming_languages_string_with_parentheses_or_empty(self, user):
        programming_languages_string = self.get_programming_languages_string(user)
        if programming_languages_string == "":
            return programming_languages_string
        else:
            return "(" + programming_languages_string + ")"

    def get_sorted_by_karma(self, other_keys):
        users = self.getByKeys("karma", "name", *other_keys)
        sorted_users = sorted(
            users,
            key=self.calculate_real_karma,
            reverse=True
        )
        return sorted_users

    def get_users_sorted_by_karma(self, vk, peer_id):
        members = self.get_members_ids(vk, peer_id)
        users = self.get_sorted_by_karma(other_keys=["programming_languages", "supporters", "opponents", "github_profile", "uid"])
        if members:
            users = [u for u in users if u["uid"] in members]
        return users

    def get_members_ids(self, vk, peer_id):
        members = self.get_members(vk, peer_id)
        if "error" in members:
            return None
        else:
            return [m["member_id"] for m in members["response"]["items"] if m["member_id"] > 0]

    def get_users_sorted_by_name(self, vk, peer_id):
        members = self.get_members_ids(vk, peer_id)
        users = self.getSortedByKeys("name", otherKeys=["programming_languages", "github_profile", "uid"])
        if members:
            users = [u for u in users if u["uid"] in members]
        users.reverse()
        return users

    def get_members(self, vk, peer_id):
        return vk.messages.getConversationMembers(peer_id=peer_id)

    def apply_user_karma(self, user, amount):
        user.karma += amount
        return (user.uid, user.name, user.karma - amount, user.karma)

    def update_user_name(self, vk, user, from_id):
        user.name = vk.users.get(user_ids=from_id)['response'][0]["first_name"]
        self.base.save(user)
