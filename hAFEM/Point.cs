namespace hAFEM
{
  public class Point
  {
    public Point(double left, double right, double middle, double h)
    {
      Left = left;
      Right = right;
      Middle = middle;
      H = h;
    }

    public double Left { get; }
    public double Right { get; }
    public double Middle { get; }
    public double H { get; }

    public double MiddleResult { get; set; }

    public double MiddleResultDeriv { get; set; }

    public double ErrorNorm { get; set; }
    public double Error { get; set; }

    public double X => Middle;
  }
}
