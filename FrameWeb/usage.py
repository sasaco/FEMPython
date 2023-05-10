import requests
import json

class usage:

    def __init__(self, production):
        self.deduct_points = 0
        if production == True:
            self.url = 'https://asia-northeast1-structuralen-395cf.cloudfunctions.net/user-firestore-usage'
        else:
            self.url = 'https://asia-northeast1-structuralengine8a9e.cloudfunctions.net/user-firestore-usage'


    def checkUser(self, js):

        body = {}

        ## ユーザーのid
        if js.uid == None:
            body['email']    = js.username   
            body['password'] = js.password
        else:
            body['uid'] = js.uid

        ## ポストする
        response = requests.post(self.url,
            json.dumps(body),
            headers={'Content-Type': 'application/json'})

        result = response.json() 
        if 'error' in result:
            return result['error']

        if result['daily_points'] > result['limit_point']:
            # すでにポイント足りないよ
            return 'The number of points that can be used at today is exceeded.'

        self.uid = result['uid']
        self.old_points = result['daily_points']
        self.limit_point = result['limit_point']
        
        return None


    def writeFirestore(self):

        content = json.dumps({
            'uid': self.uid,
            'point': self.deduct_points,
            'data': 'FrameWeb' 
        })
        # 計算ポイントをポストする
        response = requests.post(self.url,
            content,
            headers={'Content-Type': 'application/json'})

        result = response.json()  

        if 'error' in result:
            return result['error']

        self.daily_points = result['daily_points']
        
        return self.daily_points

