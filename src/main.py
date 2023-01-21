import FileIO

fileName = os.path.abspath(os.path.dirname(os.path.abspath(__file__)) + '/../examples/beam/sampleBeamHexa1.fem')
f = open(fileName, encoding="utf-8")
s = f.read()  # ファイル終端まで全て読んだデータを返す
f.close()

m = FileIO.readFemModel(s[:500])
m.init()
m.clear()
m.print_string()
m.read_string("test")
m.print_string()