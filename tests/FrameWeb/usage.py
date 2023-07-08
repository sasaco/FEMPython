import requests
import json

class usage:

    def __init__(self, production):
        self.deduct_points = 0
        self.url ="https://asia-northeast1-strcutural-engine.cloudfunctions.net/manage-point"



    def checkUser(self, js):

        body = {}

        ## ユーザーのid
        if js.uid == None:
            return 'Missing parameter: uid is required.'
        else:
            body['uid'] = js.uid

        ## ポストする
        response = requests.post(self.url,
            json.dumps(body),
            headers={'Content-Type': 'application/json'})

        result = response.json() 
        if 'error' in result:
            return result['error']

        if result['used_point'] > result['limit_point']:
            # すでにポイント足りないよ
            return 'The number of points that can be used at today is exceeded.'

        self.uid = result['uid']
        self.old_points = result['used_point']
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

        self.daily_points = result['used_point']
        
        return self.daily_points

