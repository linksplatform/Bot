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

    def calculate_real_karma(self, user):
        base_karma = user["karma"]
        up_votes = len(user["supporters"])/config.positive_votes_per_karma
        down_votes = len(user["opponents"])/config.negative_votes_per_karma
        return base_karma + up_votes - down_votes

    def get_members(self, vk, peer_id):
        return vk.messages.getConversationMembers(peer_id=peer_id)

    def get_karma_string(self, user):
        plus_string = ""
        minus_string = ""
        if isinstance(user, dict):
            karma = user["karma"]
            plus_votes = len(user["supporters"])
            minus_votes = len(user["opponents"])
        else:
            karma = user.karma
            plus_votes = len(user.supporters)
            minus_votes = len(user.opponents)
        if plus_votes > 0:
            plus_string = "+%.1f" % (plus_votes / config.positive_votes_per_karma)
        if minus_votes > 0:
            minus_string = "-%.1f" % (minus_votes / config.negative_votes_per_karma)
        if plus_votes > 0 or minus_votes > 0:
            return f"[{karma}][{plus_string}{minus_string}]"
        else:
            return f"[{karma}]"

    def get_karma_hours_limit(self, karma):
        for limit_item in config.karma_limit_hours:
            if (not limit_item["min_karma"]) or (karma >= limit_item["min_karma"]):
                if (not limit_item["max_karma"]) or (karma < limit_item["max_karma"]):
                    return limit_item["limit"]
        return 168  # hours (a week)

    def apply_karma_change(self, event, user, selected_user, operator, amount):
        selected_user_karma_change = None
        user_karma_change = None
        collective_vote_applied = None
        voters = None

        # Personal karma transfer
        if amount > 0:
            if user.karma < amount:
                self.send_not_enough_karma_error(event, user)
                return user_karma_change, selected_user_karma_change, collective_vote_applied, voters
            else:
                user_karma_change = self.apply_user_karma(user, -amount)
                amount = -amount if operator == "-" else amount
                selected_user_karma_change = self.apply_user_karma(selected_user, amount)

        # Collective vote
        elif amount == 0:
            if operator == "+":
                selected_user_karma_change, voters, collective_vote_applied = self.apply_collective_vote(user, selected_user, "supporters", config.positive_votes_per_karma, +1)
            else:
                selected_user_karma_change, voters, collective_vote_applied = self.apply_collective_vote(user, selected_user, "opponents", config.negative_votes_per_karma, -1)

        return user_karma_change, selected_user_karma_change, collective_vote_applied, voters

    def apply_collective_vote(self, user, selected_user, current_voters, number_of_voters, amount):
        vote_applied = None
        if user.uid not in selected_user[current_voters]:
            selected_user[current_voters].append(user.uid)
            vote_applied = True
        if len(selected_user[current_voters]) >= number_of_voters:
            voters = selected_user[current_voters]
            selected_user[current_voters] = []
            return self.apply_user_karma(selected_user, amount), voters, vote_applied
        return None, None, vote_applied

    def apply_user_karma(self, user, amount):
        user.karma += amount
        return (user.uid, user.name, user.karma - amount, user.karma)

    def update_user_name(self, vk, user, from_id):
        user.name = vk.users.get(user_ids=from_id)['response'][0]["first_name"]
        self.base.save(user)
