using PoohMathParser;

namespace hAFEM
{
  public class InputData
  {
    private readonly MathExpression _mu;
    private readonly MathExpression _beta;
    private readonly MathExpression _sigma;
    private readonly MathExpression _f;

    public InputData(string mu, string beta, string sigma, string f, double alpha, double eta)
    {
      _mu = new MathExpression(mu);
      _beta = new MathExpression(beta);
      _sigma = new MathExpression(sigma);
      _f = new MathExpression(f);
      Alpha = alpha;
      Eta = eta;
    }

    public int N { get; set; }

    public double H => 1.0 / N;

    public double Alpha { get; }

    public double Eta { get; }

    public double Mu(double x)
    {
      return _mu.Calculate(x);
    }

    public double Beta(double x)
    {
      return _beta.Calculate(x);
    }

    public double Sigma(double x)
    {
      return _sigma.Calculate(x);
    }

    public double F(double x)
    {
      return _f.Calculate(x);
    }
  }
}