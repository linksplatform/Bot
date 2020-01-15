from random import randint

from saya import Vk
from social_ethosa import BetterBotBase
import regex

base = BetterBotBase("users", "dat")
base.addPattern("rating", 0)
base.addPattern("quest_price", 1)
base.addPattern("current", [])
base.addPattern("current_sub", [])

class V(Vk):
    def __init__(self):
        with open("token.txt", "r") as f:
            token = f.read()
        Vk.__init__(self, token=token, group_id=190877945)

    def message_new(self, event):
        event = event["object"]["message"]
        user = base.autoInstall(event["from_id"], self) if event["from_id"] > 0 else None
        
        message = event["text"].lstrip("/")
        messages = self.get_messages(event)
        selected_message = messages[0] if len(messages) == 1 else None
        selected_user = base.autoInstall(selected_message["from_id"], self) if selected_message else None
            
        if regex.findall(r"\A\s*(рейтинг|rating)\s*\Z", message):
            self.send_rating(event, selected_user if selected_user else user, not selected_user)
        elif regex.findall(r"\A\s*(топ|top)\s*\Z", message):
            self.send_top(event)
        elif regex.findall(r"\A\s*(\+|\-)\d*\s*\Z", message):
            match = regex.match(r"\A\s*(?P<operator>\+|\-)(?P<amount>\d*)\s*\Z", message)
            operator = match.group("operator")[0]
            number = match.group("amount")
            
            n = user.quest_price
            if number:
                n = int(number)
            if not n:
                n = 0
            if operator == "-":
                n = -n
                
            if selected_user and (user.uid != selected_user.uid):
                self.send_rating_change(event, user, selected_user, operator, n)
            else:
                self.send_reward_change(event, user, operator, n)
                                           
    def send_rating_change(self, event, user, selected_user, operator, amount):
        selected_user_rating_change = None
        user_rating_change = None
        transfer = False

        if amount != 0:
            if user.rating < abs(amount):
                self.send_not_enough_rating_error(event, user)
                return None
            else:
                if user.quest_price == amount:
                    user.quest_price = 0
                user.rating -= abs(amount)
                user_rating_change = (user.name, user.rating+amount, user.rating)
                transfer = True
        else:
            if operator == "+":
                amount = 1
            else:
                amount = -1
        if operator == "+":
            if (not transfer) and (user.uid not in selected_user.current):
                selected_user.current.append(user.uid)
            if (transfer) or (len(selected_user.current) >= 2):
                if len(selected_user.current) >= 2:
                    selected_user.current = []
                selected_user.rating += amount
                selected_user_rating_change = (selected_user.name, selected_user.rating-amount, selected_user.rating)
        else:
            if (not transfer) and (user.uid not in selected_user.current_sub):
                selected_user.current_sub.append(user.uid)
            if (transfer) or (len(selected_user.current_sub) >= 3):
                if len(selected_user.current_sub) >= 3:
                    selected_user.current_sub = []
                selected_user.rating += amount
                selected_user_rating_change = (selected_user.name, selected_user.rating-amount, selected_user.rating)
        base.save(selected_user)
        if transfer:
            base.save(user)
        if selected_user_rating_change:
            if user_rating_change:
                self.send_message(event, "Рейтинг изменён: %s [%s]->[%s], %s [%s]->[%s]" % (user_rating_change + selected_user_rating_change))
            else:
                self.send_message(event, "Рейтинг изменён: %s [%s]->[%s]" % selected_user_rating_change)
                                           
    def send_reward_change(self, event, user, operator, amount):
        if operator == "+":
            if user.rating < amount:
                self.send_not_enough_rating_error(event, user)
            else:
                user.quest_price = amount
                base.save(user)
                self.send_message(event, "Вы установили награду, ваш следующий + теперь [%s]" % (amount))

    def get_messages(self, event):
        reply_message = event.get("reply_message", {})
        fwd_messages = event.get("fwd_messages", [])
        return [reply_message] if reply_message else fwd_messages
        
    def send_top(self, event):
        users = base.getSortedByKeys("rating")
        users = [i for i in users if i["rating"] != 0]
        response = "\n".join(["[id%s|%s] - [%s]" % (user["uid"], user["name"], user["rating"]) for user in users])
        self.send_message(event, response)
    
    def send_rating(self, event, user, is_self = True):
        if is_self:
            response = "%s, Ваш рейтинг - [%s]"
        else:
            response = "Рейтинг %s - [%s]"
        self.send_message(event, response % (user.name, user.rating))

    def send_not_enough_rating_error(self, event, user):
        self.send_message(event, "Извините, но вашего рейтинга [%s] недостаточно :(" % (user.rating))
                           
    def send_message(self, event, message):
        self.messages.send(message=message, peer_id=event["peer_id"], disable_mentions=1, random_id=randint(0, 1000))

if __name__ == '__main__':
    vk = V()
    print("start listen ...")
    @vk.longpoll.on_listen_end
    def restart():
        print("restart ...")
        vk.longpoll.start_listen()
    vk.start_listen()
