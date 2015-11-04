﻿Imports LibOptimization.Util
Imports LibOptimization.MathUtil

Namespace Optimization
    ''' <summary>
    ''' Real-coded Genetic Algorithm
    ''' Parent Centric Recombination(PCX)
    ''' </summary>
    ''' <remarks>
    ''' Features:
    '''  -Derivative free optimization algorithm.
    '''  -Cross over algorithm is PCX.
    '''  -Alternation of generation algorithm is G3.
    ''' 
    ''' Refference:
    ''' [1]Kalyanmoy Deb, Dhiraj Joshi and Ashish Anand, "Real-Coded Evolutionary Algorithms with Parent-Centric Recombination", KanGAL Report No. 2001003
    ''' 
    ''' Implment:
    ''' N.Tomi(tomi.nori+github at gmail.com)
    ''' </remarks>
    Public Class clsOptRealGAPCX : Inherits absOptimization
#Region "Member"
        '----------------------------------------------------------------
        'Common parameters
        '----------------------------------------------------------------
        ''' <summary>epsilon(Default:1e-8) for Criterion</summary>
        Public Property EPS As Double = 0.000000001

        ''' <summary>Use criterion</summary>
        Public Property IsUseCriterion As Boolean = True

        ''' <summary>
        ''' higher N percentage particles are finished at the time of same evaluate value.
        ''' This parameter is valid is when IsUseCriterion is true.
        ''' </summary>
        Public Property HigherNPercent As Double = 0.8 'for IsCriterion()
        Private HigherNPercentIndex As Integer = 0 'for IsCriterion())

        ''' <summary>Max iteration count</summary>
        Public Property Iteration As Integer = 10000

        '----------------------------------------------------------------
        'parameters
        '----------------------------------------------------------------
        ''' <summary>Population Size</summary>
        Public Property PopulationSize As Integer = 100

        ''' <summary>Children size</summary>
        Public Property ChildrenSize As Integer = 3

        ''' <summary>Randomize parameter Eta</summary>
        Public Property Eta As Double = 0.1

        ''' <summary>Randomize parameter Zeta</summary>
        Public Property Zeta As Double = 0.1

        ''' <summary>population</summary>
        Private m_population As New List(Of clsPoint)
#End Region

#Region "Constructor"
        ''' <summary>
        ''' Constructor
        ''' </summary>
        ''' <param name="ai_func">Optimize Function</param>
        ''' <remarks>
        ''' </remarks>
        Public Sub New(ByVal ai_func As absObjectiveFunction)
            Me.m_func = ai_func
        End Sub
#End Region

#Region "Public"
        ''' <summary>
        ''' Init
        ''' </summary>
        ''' <remarks></remarks>
        Public Overrides Sub Init()
            Try
                'init meber varibles
                Me.m_iteration = 0
                Me.m_population.Clear()

                'Set initialize value
                For i As Integer = 0 To Me.PopulationSize - 1
                    Dim temp As New List(Of Double)
                    For j As Integer = 0 To Me.m_func.NumberOfVariable - 1
                        Dim value As Double = clsUtil.GenRandomRange(Me.m_rand, -Me.InitialValueRange, Me.InitialValueRange)
                        If MyBase.InitialPosition IsNot Nothing AndAlso MyBase.InitialPosition.Length = Me.m_func.NumberOfVariable Then
                            value += Me.InitialPosition(j)
                        End If
                        temp.Add(value)
                    Next
                    Me.m_population.Add(New clsPoint(MyBase.m_func, temp))
                Next

                'Sort Evaluate
                Me.m_population.Sort()

                'Detect HigherNPercentIndex
                Me.HigherNPercentIndex = CInt(Me.m_population.Count * Me.HigherNPercent)
                If Me.HigherNPercentIndex = Me.m_population.Count OrElse Me.HigherNPercentIndex >= Me.m_population.Count Then
                    Me.HigherNPercentIndex = Me.m_population.Count - 1
                End If

            Catch ex As Exception
                Me.m_error.SetError(True, clsError.ErrorType.ERR_INIT)
            End Try
        End Sub

        ''' <summary>
        ''' Do Iteration
        ''' </summary>
        ''' <param name="ai_iteration">Iteration count. When you set zero, use the default value.</param>
        ''' <returns>True:Stopping Criterion. False:Do not Stopping Criterion</returns>
        ''' <remarks></remarks>
        Public Overrides Function DoIteration(Optional ByVal ai_iteration As Integer = 0) As Boolean
            'Check Last Error
            If Me.IsRecentError() = True Then
                Return True
            End If

            'Do Iterate
            ai_iteration = If(ai_iteration = 0, Me.Iteration - 1, ai_iteration - 1)
            For iterate As Integer = 0 To ai_iteration
                'Sort Evaluate
                Me.m_population.Sort()

                'check criterion
                If Me.IsUseCriterion = True Then
                    'higher N percentage particles are finished at the time of same evaluate value.
                    If clsUtil.IsCriterion(Me.EPS, Me.m_population(0).Eval, Me.m_population(Me.HigherNPercentIndex).Eval) Then
                        Return True
                    End If
                End If

                'Counting generation
                If Iteration <= m_iteration Then
                    Me.m_error.SetError(True, clsError.ErrorType.ERR_OPT_MAXITERATION)
                    Return True
                End If
                m_iteration += 1

                '----------------------------------------------------------------------------
                'Parent Centric Recombination
                '----------------------------------------------------------------------------
                'Alternative Starategy - G3(Genelized Generation Gap)
                Dim selectParentsIndex As List(Of Integer) = Nothing
                Dim selectParents As List(Of clsPoint) = Nothing
                Me.SelectParentsForG3(3, selectParentsIndex, selectParents)

                'PCS Crossover
                Dim newPopulation As List(Of clsPoint) = Me.CrossoverPCX(selectParents, Me.ChildrenSize, 3)

                'Replace(by G3)
                Dim replaceParent As Integer = 2
                Dim randIndex As List(Of Integer) = clsUtil.RandomPermutaion(selectParentsIndex.Count)
                For i As Integer = 0 To replaceParent - 1
                    Dim parentIndex As Integer = selectParentsIndex(randIndex(i))
                    newPopulation.Add(Me.m_population(parentIndex))
                Next
                'sort by eval
                newPopulation.Sort()

                'replace
                For i As Integer = 0 To replaceParent - 1
                    Dim parentIndex As Integer = selectParentsIndex(randIndex(i))
                    Me.m_population(parentIndex) = newPopulation(i)
                Next

                Me.m_population.Sort()
            Next

            Return False
        End Function

        ''' <summary>
        ''' Best result
        ''' </summary>
        ''' <returns>Best point class</returns>
        ''' <remarks></remarks>
        Public Overrides ReadOnly Property Result() As clsPoint
            Get
                Return Me.m_population(0)
            End Get
        End Property

        ''' <summary>
        ''' Get recent error
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Overrides Function IsRecentError() As Boolean
            Return Me.m_error.IsError()
        End Function

        ''' <summary>
        ''' All Result
        ''' </summary>
        ''' <value></value>
        ''' <returns></returns>
        ''' <remarks>
        ''' for Debug
        ''' </remarks>
        Public Overrides ReadOnly Property ResultForDebug As List(Of clsPoint)
            Get
                Return Me.m_population
            End Get
        End Property
#End Region

#Region "Private Methods"
        ''' <summary>
        ''' Select parent for G3(Genelized Generation Gap)
        ''' </summary>
        ''' <param name="ai_pickN"></param>
        ''' <param name="ao_parentIndex"></param>
        ''' <param name="ao_retParents"></param>
        ''' <remarks></remarks>
        Private Sub SelectParentsForG3(ByVal ai_pickN As Integer, ByRef ao_parentIndex As List(Of Integer), ByRef ao_retParents As List(Of clsPoint))
            'generate random permutation array without best parent index
            Dim randIndex As List(Of Integer) = clsUtil.RandomPermutaion(Me.m_population.Count, 0)

            'generate random permutation with best parent index
            ao_parentIndex = New List(Of Integer)(ai_pickN)
            ao_retParents = New List(Of clsPoint)(ai_pickN)
            Dim insertBestParentPosition As Integer = Me.m_rand.Next(0, ai_pickN)
            For i As Integer = 0 To ai_pickN - 1
                If i = insertBestParentPosition Then
                    ao_parentIndex.Add(0)
                    ao_retParents.Add(Me.m_population(0))
                Else
                    ao_parentIndex.Add(randIndex(i))
                    ao_retParents.Add(Me.m_population(randIndex(i)))
                End If
            Next
        End Sub

        ''' <summary>
        ''' Crossover PCX
        ''' </summary>
        ''' <param name="ai_parents"></param>
        ''' <param name="ai_childrenSize"></param>
        ''' <param name="ai_pickParentNo"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function CrossoverPCX(ByVal ai_parents As List(Of clsPoint), ByVal ai_childrenSize As Integer, ByVal ai_pickParentNo As Integer) As List(Of clsPoint)
            Dim retPopulation As New List(Of clsPoint)

            For pNo As Integer = 0 To ai_childrenSize - 1
                Dim randIndex As List(Of Integer) = clsUtil.RandomPermutaion(ai_parents.Count)
                Dim parentsPoint As New List(Of clsPoint)(ai_pickParentNo)
                For i As Integer = 0 To ai_pickParentNo - 1
                    parentsPoint.Add(ai_parents(randIndex(i)))
                Next

                'calc g
                Dim g As New clsEasyVector(Me.m_func.NumberOfVariable)
                For i As Integer = 0 To ai_pickParentNo - 1
                    g += parentsPoint(i)
                Next
                g /= ai_pickParentNo

                'calc D
                Dim d As clsEasyVector = g - parentsPoint(0)
                Dim dist As Double = d.NormL2()
                If dist < Me.EPS Then
                    'Console.WriteLine("very near! g")
                End If

                Dim diff As New List(Of clsEasyVector)
                For i As Integer = 1 To ai_pickParentNo - 1
                    diff.Add(New clsEasyVector(Me.m_func.NumberOfVariable))
                    diff(i - 1) = parentsPoint(i) - parentsPoint(0)
                    If diff(i - 1).NormL2 < EPS Then
                        'Console.WriteLine("very near!")
                    End If
                Next

                'orthogonal directions -> Vector D
                Dim DD As New clsEasyVector(Me.m_func.NumberOfVariable)
                For i As Integer = 0 To ai_pickParentNo - 2
                    Dim temp1 As Double = diff(i).InnerProduct(d)
                    Dim temp2 As Double = temp1 / (diff(i).NormL2 * dist)
                    Dim temp3 = 1.0 - Math.Pow(temp2, 2.0)
                    DD(i) = diff(i).NormL2 * Math.Sqrt(temp3)
                Next

                'Average vector D
                Dim meanD As Double = DD.Average()
                Dim tempV1 As New clsEasyVector(Me.m_func.NumberOfVariable)
                For i As Integer = 0 To Me.m_func.NumberOfVariable - 1
                    tempV1(i) = clsUtil.NormRand(0.0, meanD * Eta)
                Next
                Dim tempInnerP As Double = tempV1.InnerProduct(d)
                Dim tempNRand As Double = clsUtil.NormRand(0.0, Zeta)
                For i As Integer = 0 To Me.m_func.NumberOfVariable - 1
                    tempV1(i) = tempV1(i) - tempInnerP * DD(i) / Math.Pow(dist, 2.0)
                    tempV1(i) += tempNRand * d(i)
                Next

                'add population
                Dim tempChildVector As clsEasyVector = parentsPoint(0) + tempV1
                retPopulation.Add(New clsPoint(Me.m_func, tempChildVector))
            Next

            Return retPopulation
        End Function
#End Region
    End Class
End Namespace
