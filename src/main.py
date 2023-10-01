import json
from calcrate import FrameCalc
from dataInput import dataInput
from usage import usage
import functions_framework

@functions_framework.http
def FrameWeb(request):

    # Set CORS headers for the preflight request
    if request.method == 'OPTIONS':
        headers = {
            'Access-Control-Allow-Origin': '*',
            'Access-Control-Allow-Methods': 'GET, POST',
            'Access-Control-Allow-Headers': 'Origin, X-Requested-With, Content-Type, Accept, Content-Encoding, Authorization',
            'Access-Control-Max-Age': '3600'
        }

        return ('', 204, headers)

    # Set CORS headers for the main request
    headers = {
        'Access-Control-Allow-Origin': '*',
        'Access-Control-Allow-Headers': 'Origin, X-Requested-With, Content-Type, Accept, Content-Encoding, Authorization'
    }

    if request.method == 'GET':
        return (json.dumps({ 'results': 'Hello World!'}), 200, headers) # テスト用コード

    # データの送受信企画を調べる
    encoding = 'json'
    if 'content-encoding' in request.headers:
        encoding = request.headers['content-encoding']
        
    # 計算開始
    try:
        # 入力データを読み込む
        inp = dataInput()
        if encoding == 'json':
            js = request.get_json()
            error = inp.setJSON(js)
        else:
            error = inp.setCompressJSON(request.data)
        if error != None:
            return (json.dumps({'error': error}), 200, headers)

        calc = FrameCalc(inp)

        # ユーザーチェック 
        user = usage(inp.production)
        error = user.checkUser(inp)
        if error != None:
            return (json.dumps(error), 200, headers)

        # 計算開始
        error, result = calc.calcrate()
        if error != None:
            return (json.dump({'error': error}), 200, headers)
            
        # 使用量を計算する
        for key in result:
            re = result[key]
            user.deduct_points += re['size']
            
        result['old_points'] = user.old_points
        result['deduct_points'] = user.deduct_points
        result['new_points'] = user.writeFirestore()

        # 結果を返送する
        strJson = json.dumps(result)

        if encoding == 'json':
            response = strJson
        else:
            response = inp.compress(strJson)

        return (response, 200, headers)


    except Exception as e:
        return (json.dumps({'error': e}), 200, headers)



