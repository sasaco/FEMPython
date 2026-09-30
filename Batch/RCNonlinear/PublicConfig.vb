Module PublicConfig
    ''' <summary>釣り合い計算の際の無限ループを回避するリミッター</summary>
    Public Const LIMITERVALUE1 As Double = 0.001
    Public Const LIMITERVALUE2 As Double = 1000
    ''' <summary>釣り合い計算の際のRC断面の分割数</summary>
    Public Const NSVALUE1 As Integer = 100
    ''' <summary>回転角を計算する際の積分分割数</summary>
    Public Const NSVALUE2 As Integer = 20
    ''' <summary>軸力変動を考慮する際の設計軸力の分割数</summary>
    Public Const NDCOUNT As Integer = 20
    ''' <summary>円形断面の断面耐力を計算する際の断面分割数</summary>
    Public Const RCOUNT As Integer = 20
End Module
