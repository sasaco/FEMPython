#import sys
#sys.path.append('../src/')
from main import FEMPython

calc = FEMPython()
model = calc.model
model.clear()

import json

from calcrate import FrameCalc
from dataInput import dataInput
from usage import usage

def test_out_json(out_text):
    fout=open('test.out.json', 'w')
    print(out_text, file=fout)
    fout.close()
    exit()




# 入力データを読み込む
inp = dataInput()
inp.readTestData('test.json')

calc = FrameCalc(inp)

# ユーザーチェック 
# user = usage(inp.production)
# error = user.checkUser(inp)
# if error != None:
#     test_out_json(json.dumps({'error': error['message']}))

    
# 計算開始
error, result = calc.calcrate()
if error != None:
    test_out_json(json.dumps({'error': error}))
    
# # 使用量を計算する
# for key in result:
#     re = result[key]
#     user.deduct_points += re['size']
    
# result['old_points'] = user.old_points
# result['deduct_points'] = user.deduct_points
# result['new_points'] = user.writeFirestore()

# 結果を返送する

test_out_json(json.dumps(result))
