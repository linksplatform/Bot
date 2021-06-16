from social_ethosa import BetterBotBase

сlass BetterBotBaseDataService:
       base = BetterBotBase("users", "dat")
       base.addPattern("karma", 0)
       base.addPattern("programming_languages", [])
       base.addPattern("github_profile", "")
       base.addPattern("supporters", [])
       base.addPattern("opponents", [])
       base.addPattern("last_collective_vote", 0)

       self.base = base
