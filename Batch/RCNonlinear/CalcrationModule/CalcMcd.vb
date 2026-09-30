''' <summary>
''' Ｃ点の曲げモーメントＭc と 部材角φc を計算するモジュール
''' </summary>
Module CalcMcd

    ''' <summary>Ｃ点の曲げモーメントＭc と 部材角φc</summary>
    ''' <param name="Indata">入力データClass InputData</param>
    ''' <returns>結果データClass clsReactions</returns>
    Public Function φcd(ByVal Indata As InputData) As clsReaction
        Dim result As New clsReaction

        'Mc 
        result.Mi = Mcd(Indata)
        If result.Mi < 0 Then
            result.Mi = 0
            Return result '軸力が小さすぎ 解なし
        End If

        'φc = Mc／(Ec・Ie)
        result.fi = 1000 * result.Mi / (Indata.Sc.Ec * Indata.Ie)

        'θc = φc・La／3
        If Double.IsNaN(Indata.La) = False Then
            result.ri = result.fi * Indata.La / 3
        End If

        Return result
    End Function

    ''' <summary>
    ''' 曲げひび割れ発生時の曲げモーメントで、コンクリートの縁引張応力度が部材寸法の影響を
    ''' 考慮した設計曲げ強度fbdに達するときの曲げモーメントとする。
    ''' </summary>
    Public Function Mcd(ByVal Indata As InputData) As Double

        ''*** 断面の基本情報を計算する。
        Dim Hc As Double = Indata.Hc        'コンクリート断面高さ
        Dim Ass As Double = Indata.Ass      '鉄筋の全断面積
        Dim Ae As Double = Indata.Ae        'コンクリートの有効断面積
        Dim Z As Double = Indata.Z          '断面係数
        Dim Es As Double = Indata.Se.Es     '鉄筋のヤング係数
        Dim Ec As Double = Indata.Sc.Ec     'コンクリートのヤング係数


        '*** fbck は次式により算定する。
        Dim fbck As Double
        Try
            With Indata.Sc
                '*** コンクリート曲げ強度の特性値ftbは, 次式により算定する。
                Dim fck As Double = .fck * .rm
                Dim ftk As Double = .ftk

                '*** fbdを算定する際の係数 k0bは, 次式により算定する。
                Dim Gf As Double = 1 / 100 * (.dmax ^ (1 / 3)) * (fck ^ (1 / 3))
                Dim lch As Double = 1000 * Gf * .Ec / (ftk ^ 2)
                Dim k0b As Double = 1 + 1 / (0.85 + (4.5 * (Hc / lch)))

                '*** fbdを算定する際の係数 k1bは, 次式により算定する。
                Dim k1b As Double = Math.Max(0.55 / ((Hc / 1000) ^ (1 / 4)), 0.4)

                '*** fbd は次式により算定する。
                fbck = k0b * k1b * ftk / .rc
            End With
        Catch ex As Exception : Throw ex
        End Try

        '*** 曲げひび割れ発生時の曲げモーメントＭｃは次式により算定する。
        Try
            Return (Z * (fbck + (Indata.Nd * 1000 / (Ae + (Ass * Es / Ec)))) / 1000000)
        Catch ex As Exception : Throw ex
        End Try
    End Function

    ''' <summary>
    ''' 軸引張ひび割れNcr(軸力のみによってひび割れ発生する引張力)
    ''' </summary>
    ''' <param name="Indata"></param>
    ''' <returns></returns>
    Public Function Ncr(Indata As InputData) As Double
        ''*** 断面の基本情報を計算する。
        Dim Hc As Double = Indata.Hc        'コンクリート断面高さ
        Dim Ass As Double = Indata.Ass      '鉄筋の全断面積
        Dim Ae As Double = Indata.Ae        'コンクリートの有効断面積
        Dim Es As Double = Indata.Se.Es     '鉄筋のヤング係数
        Dim Ec As Double = Indata.Sc.Ec     'コンクリートのヤング係数
        '*** fbck は次式により算定する。
        Dim fbck As Double
        Try
            With Indata.Sc
                '*** コンクリート曲げ強度の特性値ftbは, 次式により算定する。
                Dim fck As Double = .fck * .rm
                Dim ftk As Double = .ftk

                '*** fbdを算定する際の係数 k0bは, 次式により算定する。
                Dim Gf As Double = 1 / 100 * (.dmax ^ (1 / 3)) * (fck ^ (1 / 3))
                Dim lch As Double = 1000 * Gf * .Ec / (ftk ^ 2)
                Dim k0b As Double = 1 + 1 / (0.85 + (4.5 * (Hc / lch)))

                '*** fbdを算定する際の係数 k1bは, 次式により算定する。
                Dim k1b As Double = Math.Max(0.55 / ((Hc / 1000) ^ (1 / 4)), 0.4)

                '*** fbd は次式により算定する。
                fbck = k0b * k1b * ftk / .rc
            End With
            Return (-fbck * (Ae + (Ass * Es / Ec))) / 1000
        Catch ex As Exception : Throw ex
        End Try
    End Function

End Module
