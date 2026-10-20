
Option Strict On
Option Explicit On

Imports System
Imports System.Collections.Generic
Imports System.Linq

Public Enum OptionSide
    CE
    PE
End Enum

Public Class NiftyCandle
    Public Property TimeIST As DateTime
    Public Property Open As Decimal
    Public Property High As Decimal
    Public Property Low As Decimal
    Public Property Close As Decimal
    Public Property Volume As Decimal
End Class

Public Class OptionQuote
    Public Property Symbol As String = ""
    Public Property Strike As Decimal
    Public Property Side As OptionSide
    Public Property LastTradedPrice As Decimal
End Class

Public Class NiftyOptionSignal
    Public Property Symbol As String = ""
    Public Property Side As OptionSide
    Public Property Strike As Decimal
    Public Property NiftyPrice As Decimal
    Public Property EntryPremium As Decimal
    Public Property StopLoss As Decimal
    Public Property Target As Decimal
    Public Property EMA9 As Decimal
    Public Property EMA21 As Decimal
    Public Property VWAP As Decimal
    Public Property SignalTime As DateTime
End Class

Public Class NiftyOptionsSignalService

    Private ReadOnly _strikeStep As Decimal
    Private ReadOnly _stopLossPercent As Decimal
    Private ReadOnly _riskReward As Decimal

    Public Sub New(Optional strikeStep As Decimal = 50D,
                   Optional stopLossPercent As Decimal = 20D,
                   Optional riskReward As Decimal = 1.5D)

        If strikeStep <= 0D Then
            Throw New ArgumentOutOfRangeException(NameOf(strikeStep))
        End If

        If stopLossPercent <= 0D OrElse stopLossPercent >= 100D Then
            Throw New ArgumentOutOfRangeException(NameOf(stopLossPercent))
        End If

        If riskReward <= 0D Then
            Throw New ArgumentOutOfRangeException(NameOf(riskReward))
        End If

        _strikeStep = strikeStep
        _stopLossPercent = stopLossPercent
        _riskReward = riskReward
    End Sub

    Public Function GetATMStrike(niftyPrice As Decimal) As Decimal
        If niftyPrice <= 0D Then
            Throw New ArgumentOutOfRangeException(NameOf(niftyPrice))
        End If

        Return Math.Round(
            niftyPrice / _strikeStep,
            MidpointRounding.AwayFromZero) * _strikeStep
    End Function

    Private Function CalculateEMA(
        candles As IReadOnlyList(Of NiftyCandle),
        period As Integer) As Decimal()

        Dim result(candles.Count - 1) As Decimal
        Dim multiplier As Decimal = 2D / (period + 1D)

        result(0) = candles(0).Close

        For i As Integer = 1 To candles.Count - 1
            result(i) = candles(i).Close * multiplier +
                        result(i - 1) * (1D - multiplier)
        Next

        Return result
    End Function

    Private Function CalculateVWAP(
        candles As IReadOnlyList(Of NiftyCandle),
        index As Integer) As Decimal

        Dim sessionDate As Date = candles(index).TimeIST.Date
        Dim totalPV As Decimal = 0D
        Dim totalVolume As Decimal = 0D

        For i As Integer = 0 To index
            If candles(i).TimeIST.Date <> sessionDate Then Continue For

            Dim typicalPrice As Decimal =
                (candles(i).High + candles(i).Low +
                 candles(i).Close) / 3D

            totalPV += typicalPrice * candles(i).Volume
            totalVolume += candles(i).Volume
        Next

        If totalVolume = 0D Then Return 0D

        Return totalPV / totalVolume
    End Function

    Public Function Evaluate(
        candles As IReadOnlyList(Of NiftyCandle),
        callQuote As OptionQuote,
        putQuote As OptionQuote) As NiftyOptionSignal

        If candles Is Nothing OrElse candles.Count < 50 Then
            Return Nothing
        End If

        Dim index As Integer = candles.Count - 1
        Dim current As NiftyCandle = candles(index)
        Dim previous As NiftyCandle = candles(index - 1)

        ' This service expects completed candles in chronological order.
        If current.TimeIST.TimeOfDay < New TimeSpan(9, 20, 0) OrElse
           current.TimeIST.TimeOfDay > New TimeSpan(14, 30, 0) Then
            Return Nothing
        End If

        Dim ema9 = CalculateEMA(candles, 9)
        Dim ema21 = CalculateEMA(candles, 21)
        Dim vwap As Decimal = CalculateVWAP(candles, index)

        Dim bullish As Boolean =
            ema9(index) > ema21(index) AndAlso
            current.Close > vwap AndAlso
            current.Close > previous.High

        Dim bearish As Boolean =
            ema9(index) < ema21(index) AndAlso
            current.Close < vwap AndAlso
            current.Close < previous.Low

        If Not bullish AndAlso Not bearish Then Return Nothing

        Dim side As OptionSide = If(bullish, OptionSide.CE, OptionSide.PE)
        Dim quote As OptionQuote = If(bullish, callQuote, putQuote)

        If quote Is Nothing OrElse quote.Side <> side Then Return Nothing
        If quote.LastTradedPrice <= 0D Then Return Nothing

        Dim atmStrike As Decimal = GetATMStrike(current.Close)

        If quote.Strike <> atmStrike Then Return Nothing

        Dim entry As Decimal = quote.LastTradedPrice
        Dim stopLoss As Decimal =
            entry * (1D - _stopLossPercent / 100D)
        Dim target As Decimal =
            entry + (entry - stopLoss) * _riskReward

        Return New NiftyOptionSignal With {
            .Symbol = quote.Symbol,
            .Side = side,
            .Strike = atmStrike,
            .NiftyPrice = current.Close,
            .EntryPremium = Decimal.Round(entry, 2),
            .StopLoss = Decimal.Round(stopLoss, 2),
            .Target = Decimal.Round(target, 2),
            .EMA9 = ema9(index),
            .EMA21 = ema21(index),
            .VWAP = vwap,
            .SignalTime = current.TimeIST
        }
    End Function

End Class
