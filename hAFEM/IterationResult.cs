using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace hAFEM
{
  public class IterationResult
  {
    public int IterationNumber { get; set; }

    public int ElementsNumber => Points.Count;

    public List<Point> Points { get; set; }

    public List<double> Results { get; set; }

    public List<double> Errors { get; set; }

    public double ErrorNormSquare { get; set; }

    public double UNormSquare { get; set; }

    public void SetResults(double[] results)
    {
      Results = new List<double>(new double[Points.Count + 1]);
      if (Results.Count != results.Length)
      {
        throw new ArgumentOutOfRangeException(nameof(results));
      }

      for (var i = 0; i < Points.Count; i++)
      {
        Results[i] = results[i];
        Points[i].MiddleResult = 0.5 * (results[i + 1] + results[i]);
        Points[i].MiddleResultDeriv = (results[i + 1] - results[i]) / Points[i].H;
      }
    }
  }
}
