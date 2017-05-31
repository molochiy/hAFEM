using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using ZedGraph;

namespace hAFEM
{
  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow
  {

    private List<IterationResult> _iterationResults;
    private InputData _inputData;

    public MainWindow()
    {
      InitializeComponent();
      InitializeInputData();
      InitializeGraphPane();
    }

    private void InitializeInputData()
    {
      MuTxbx.Text = "0,0025";
      BetaTxbx.Text = "0";
      SigmaTxbx.Text = "1";
      FTxbx.Text = "cos(pi*x)*cos(pi*x)+0,005*pi*pi*cos(2*pi*x)";
      AlphaTxbx.Text = "1000";
      EtaTxbx.Text = "10";
      NTxbx.Text = "4";

      //MuTxbx.Text = "1";
      //BetaTxbx.Text = "100";
      //SigmaTxbx.Text = "0";
      //FTxbx.Text = "100";
      //AlphaTxbx.Text = "100000";
      //EtaTxbx.Text = "5";
      //NTxbx.Text = "4";
    }

    public void InitializeGraphPane()
    {
      FunctionResultGraph.GraphPane.XAxis.Scale.Min = 0;
      FunctionResultGraph.GraphPane.XAxis.Scale.Max = 1;
      FunctionResultGraph.GraphPane.YAxis.Scale.Min = 0;
      FunctionResultGraph.GraphPane.YAxis.Scale.Max = 10;
      FunctionResultGraph.GraphPane.Title.Text = "";
      FunctionResultGraph.GraphPane.XAxis.Title.Text = "x";
      FunctionResultGraph.GraphPane.YAxis.Title.Text = "u(x)";
      FunctionResultGraph.IsShowPointValues = true;
      FunctionResultGraph.AxisChange();
      FunctionResultGraph.Invalidate();

      ErrorResultGraph.GraphPane.XAxis.Scale.Min = 0;
      ErrorResultGraph.GraphPane.XAxis.Scale.Max = 1;
      ErrorResultGraph.GraphPane.YAxis.Scale.Min = 0;
      ErrorResultGraph.GraphPane.YAxis.Scale.Max = 10;
      ErrorResultGraph.GraphPane.Title.Text = "";
      ErrorResultGraph.GraphPane.XAxis.Title.Text = "x";
      ErrorResultGraph.GraphPane.YAxis.Title.Text = "η, in %";
      ErrorResultGraph.IsShowPointValues = true;
      ErrorResultGraph.AxisChange();
      ErrorResultGraph.Invalidate();
    }

    public void InitializeProblem()
    {
      _inputData = new InputData(MuTxbx.Text, BetaTxbx.Text, SigmaTxbx.Text, FTxbx.Text, double.Parse(AlphaTxbx.Text), double.Parse(EtaTxbx.Text)) { N = int.Parse(NTxbx.Text) };
      _iterationResults = new List<IterationResult>();
      var iterationResult = new IterationResult
      {
        IterationNumber = 1,
        Points = new List<Point>()
      };

      for (var i = 0; i < _inputData.N - 1; i++)
      {
        var currPoint = i * _inputData.H;
        iterationResult.Points.Add(new Point(currPoint, currPoint + _inputData.H, currPoint + _inputData.H / 2, _inputData.H));
      }

      var prevPoint = iterationResult.Points.Last();

      iterationResult.Points.Add(new Point(prevPoint.Right, 1, prevPoint.Right + _inputData.H / 2, _inputData.H));

      _iterationResults.Add(iterationResult);
    }

    private void CalculateBtn_Click(object sender, RoutedEventArgs e)
    {
      InitializeProblem();
      while (Calculate()) { }
      ShowResults();
    }

    private bool Calculate()
    {
      var matrix = InitializeMatrix();
      var vector = InitializeVector();

      var results = Progonka(matrix, vector);

      var lastIteration = _iterationResults.Last();
      lastIteration.SetResults(results);

      CalculateError();

      return CheckForErrorsDeviation();
    }

    private double[,] InitializeMatrix()
    {
      var lastIteration = _iterationResults.Last();
      var points = lastIteration.Points;

      var n = lastIteration.ElementsNumber;

      var matrix = new double[n + 1, n + 1];

      //matrix[0, 0] = (_inputData.Mu(points[0].Middle) / points[0].H) * (-1 + (1.0 / 6) * Pe(points[0].H, points[0].Middle) * (2 * Sh(points[0].H, points[0].Middle) + 3)) + _inputData.Alpha;
      //matrix[0, 1] = (_inputData.Mu(points[0].Middle) / points[0].H) * (-1 + (1.0 / 6) * Pe(points[0].H, points[0].Middle) * (2 * Sh(points[0].H, points[0].Middle) - 3));

      for (int i = 1; i < n; ++i)
      {
        matrix[i, i - 1] = (_inputData.Mu(points[i - 1].Middle) / points[i - 1].H)
                         * (-1 + (1.0 / 6) * Pe(points[i - 1].H, points[i - 1].Middle) * (Sh(points[i - 1].H, points[i - 1].Middle) - 3));
        matrix[i, i] = -1.0 * ((_inputData.Mu(points[i - 1].Middle) / points[i - 1].H) * (-1 + (1.0 / 6) * Pe(points[i - 1].H, points[i - 1].Middle) * (2 * Sh(points[i - 1].H, points[i - 1].Middle) + 3))
                             + (_inputData.Mu(points[i].Middle) / points[i].H) * (-1 + (1.0 / 6) * Pe(points[i].H, points[i].Middle) * (2 * Sh(points[i].H, points[i].Middle) - 3)));
        matrix[i, i + 1] = (_inputData.Mu(points[i].Middle) / points[i].H) * (-1 + (1.0 / 6) * Pe(points[i].H, points[i].Middle) * (Sh(points[i].H, points[i].Middle) + 3));
      }

      matrix[n, n - 1] = (_inputData.Mu(points[n - 1].Middle) / points[n - 1].H) * (-1 + (1.0 / 6) * Pe(points[n - 1].H, points[n - 1].Middle) * (2 * Sh(points[n - 1].H, points[n - 1].Middle) - 3));
      matrix[n, n] = (_inputData.Mu(points[n - 1].Middle) / points[n - 1].H) * (-1 + (1.0 / 6) * Pe(points[n - 1].H, points[n - 1].Middle) * (2 * Sh(points[n - 1].H, points[n - 1].Middle) + 3)) + _inputData.Alpha;

      return matrix;
    }

    private double[] InitializeVector()
    {
      var lastIteration = _iterationResults.Last();
      var points = lastIteration.Points;

      var n = lastIteration.ElementsNumber;

      var vector = new double[n + 1];

      vector[0] = 0;

      for (int i = 1; i < n; ++i)
      {
        vector[i] = 0.5 * (points[i - 1].H * _inputData.F(points[i - 1].Middle) + points[i].H * _inputData.F(points[i].Middle));
      }

      vector[n] = 0.5 * points[n - 1].H * _inputData.F(points[n - 1].Middle);

      return vector;
    }

    public double Pe(double h, double x)
    {
      return h * _inputData.Beta(x) / _inputData.Mu(x);
    }

    public double Sh(double h, double x)
    {
      return h * h * _inputData.Sigma(x) / _inputData.Mu(x);
    }

    public double[] Progonka(double[,] matrix, double[] vector)
    {
      var n = vector.Length;
      var q = new double[n];
      var a = new double[n];
      var b = new double[n];

      a[0] = Math.Abs(matrix[0, 0]) > double.Epsilon ? -matrix[0, 1] / matrix[0, 0] : 0;
      b[0] = Math.Abs(matrix[0, 0]) > double.Epsilon ? vector[0] / matrix[0, 0] : 0;

      for (int i = 1; i < n - 1; i++)
      {
        a[i] = -matrix[i, i + 1] / (matrix[i, i - 1] * a[i - 1] + matrix[i, i]);
        b[i] = (vector[i] - matrix[i, i - 1] * b[i - 1]) / (matrix[i, i - 1] * a[i - 1] + matrix[i, i]);
      }

      b[n - 1] = (vector[n - 1] - matrix[n - 1, n - 2] * b[n - 2]) / (matrix[n - 1, n - 2] * a[n - 2] + matrix[n - 1, n - 1]);

      q[n - 1] = b[n - 1];

      for (int i = n - 2; i >= 0; i--)
      {
        q[i] = b[i] + a[i] * q[i + 1];
      }

      return q;
    }

    public void CalculateError()
    {
      var lastIteration = _iterationResults.Last();
      var points = lastIteration.Points;

      var n = lastIteration.ElementsNumber;

      double errorNormSquare = 0.0;
      double uNormSquare = 0.0;


      // TODO: ERROR NORM???
      for (var i = 0; i < n; i++)
      {
        var m = points[i].H * points[i].H * points[i].H / _inputData.Mu(points[i].Middle);
        var b = _inputData.F(points[i].Middle) - _inputData.Beta(points[i].Middle) * points[i].MiddleResultDeriv - _inputData.Sigma(points[i].Middle) * points[i].MiddleResult;
        var d = 10 + Pe(points[i].H, points[i].Middle) * Sh(points[i].H, points[i].Middle);
        var errorNorm = 1.25 * (points[i].H * points[i].H / _inputData.Mu(points[i].Middle)) * b / d;

        points[i].ErrorNorm = (m * b * b) / d;//Math.Abs(errorNorm);

        errorNormSquare += (m * b * b) / d;

        //uNormV2_i
        uNormSquare += points[i].H * points[i].MiddleResultDeriv * points[i].MiddleResultDeriv;
      }

      errorNormSquare *= 5.0 / 6;

      lastIteration.ErrorNormSquare = errorNormSquare;
      lastIteration.UNormSquare = uNormSquare;

      for (var i = 0; i < n; i++)
      {
        points[i].Error = Math.Sqrt(n * points[i].ErrorNorm / (errorNormSquare + uNormSquare)) * 100;
      }
    }

    public bool CheckForErrorsDeviation()
    {
      var isDeviation = false;

      var lastIteration = _iterationResults.Last();
      var points = lastIteration.Points;

      var iterationResult = new IterationResult
      {
        IterationNumber = _iterationResults.Count + 1,
        Points = new List<Point>()
      };

      foreach (var point in points)
      {
        if (point.Error >= _inputData.Eta)
        {
          isDeviation = true;
          iterationResult.Points.Add(new Point(point.Left, point.Middle, (point.Left + point.Middle) / 2, point.Middle - point.Left));
          iterationResult.Points.Add(new Point(point.Middle, point.Right, (point.Middle + point.Right) / 2, point.Right - point.Middle));
        }
        else
        {
          iterationResult.Points.Add(point);
        }
      }

      if (isDeviation)
      {
        _iterationResults.Add(iterationResult);
      }

      return isDeviation;
    }

    public void ShowResults()
    {
      FillIterationResultDataGrid();
      DrawGraphs(0);
      FillDataGrids(0);
    }

    private void FillIterationResultDataGrid()
    {
      IterationResultDtGrd.ItemsSource = _iterationResults;
    }

    private void DrawGraphs(int iterationNumber)
    {
      var iteration = _iterationResults[iterationNumber];

      DrawFunctionResultGraph(iteration);
      DrawErrorResultGraph(iteration);
    }

    private void DrawFunctionResultGraph(IterationResult iteration)
    {
      FunctionResultGraph.GraphPane.CurveList.Clear();
      PointPairList numList = new PointPairList();
      FunctionResultGraph.GraphPane.XAxis.Scale.Min = 0;
      FunctionResultGraph.GraphPane.XAxis.Scale.Max = 1;
      double min = iteration.Results.Min();
      double max = iteration.Results.Max();
      FunctionResultGraph.GraphPane.YAxis.Scale.Min = min - 0.25 * Math.Abs(min);
      FunctionResultGraph.GraphPane.YAxis.Scale.Max = max + 0.25 * Math.Abs(max);
      FunctionResultGraph.GraphPane.Title.Text = iteration.Points.Count + " Finite Elements";
      FunctionResultGraph.AxisChange();
      FunctionResultGraph.Invalidate();
      for (int i = 0; i < iteration.Points.Count; ++i)
      {
        numList.Add(iteration.Points[i].Left, iteration.Results[i]);
      }
      numList.Add(iteration.Points[iteration.Points.Count - 1].Right, iteration.Results[iteration.Points.Count]);
      FunctionResultGraph.GraphPane.AddCurve("", numList, System.Drawing.Color.Blue, SymbolType.Star);
      FunctionResultGraph.AxisChange();
      FunctionResultGraph.Invalidate();
    }

    private void DrawErrorResultGraph(IterationResult iteration)
    {
      ErrorResultGraph.GraphPane.CurveList.Clear();
      PointPairList numList = new PointPairList();
      ErrorResultGraph.GraphPane.XAxis.Scale.Min = 0;
      ErrorResultGraph.GraphPane.XAxis.Scale.Max = 1;
      double min = iteration.Points.Min(p => p.Error);
      double max = iteration.Points.Max(p => p.Error);
      ErrorResultGraph.GraphPane.YAxis.Scale.Min = Math.Min(min, _inputData.Eta) - 5;
      ErrorResultGraph.GraphPane.YAxis.Scale.Max = Math.Max(max, _inputData.Eta) + 5;
      ErrorResultGraph.GraphPane.Title.Text = "Finite Elements Error";
      ErrorResultGraph.AxisChange();
      ErrorResultGraph.Invalidate();
      foreach (Point point in iteration.Points)
      {
        numList.Add(point.Middle, point.Error);
      }

      ErrorResultGraph.GraphPane.AddCurve("", numList, System.Drawing.Color.Blue, SymbolType.Star);
      ErrorResultGraph.GraphPane.AddCurve("", new PointPairList { { -100, _inputData.Eta }, { 100, _inputData.Eta } }, System.Drawing.Color.Red, SymbolType.None);
      ErrorResultGraph.AxisChange();
      ErrorResultGraph.Invalidate();
    }

    private void FillDataGrids(int iterationNumber)
    {
      var iteration = _iterationResults[iterationNumber];

      FillFunctionResultDtGrd(iteration);
      FillErrorResultDtGrd(iteration);
    }

    private void FillFunctionResultDtGrd(IterationResult iteration)
    {
      //FunctionResultDtGrd.ItemsSource = null;
      var points = iteration.Points.Select(p => p.Left).ToList();
      points.Add(iteration.Points.Last().Right);
      var results = points.Zip(iteration.Results, (point, result) => new { X = point, Result = result });
      FunctionResultDtGrd.ItemsSource = results;
    }

    private void FillErrorResultDtGrd(IterationResult iteration)
    {
      //ErrorResultDtGrd.Items.Clear();
      ErrorResultDtGrd.ItemsSource = iteration.Points;
    }

    private void ShowSolution(object sender, RoutedEventArgs e)
    {
      var iteration = ((FrameworkElement)sender).DataContext as IterationResult;

      DrawGraphs(iteration.IterationNumber - 1);
      FillDataGrids(iteration.IterationNumber - 1);
    }
  }
}
