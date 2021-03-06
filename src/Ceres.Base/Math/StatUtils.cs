#region License notice

/*
  This file is part of the Ceres project at https://github.com/dje-dev/ceres.
  Copyright (C) 2020- by David Elliott and the Ceres Authors.

  Ceres is free software under the terms of the GNU General Public License v3.0.
  You should have received a copy of the GNU General Public License
  along with Ceres. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

#region Using directive

using Ceres.Base.DataTypes;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

#endregion

namespace Ceres.Base.Math
{
  /// <summary>
  /// Collection of miscellaneous statistical utility methods.
  /// </summary>
  public static class StatUtils
  {
    /// <summary>
    /// Returns the value bounded within the range [min, max].
    /// </summary>
    /// <param name="value"></param>
    /// <param name="min"></param>
    /// <param name="max"></param>
    /// <returns></returns>
    public static float Bounded(float value, float min, float max)
    {
      if (value < min)
        return min;
      else if (value > max)
        return max;
      else
        return value;
    }

    /// <summary>
    /// Returns average of an array of floats.
    /// </summary>
    /// <param name="xs"></param>
    /// <returns></returns>
    public static float Average(params float[] xs)
    {
      float tot = 0;
      for (int i = 0; i < xs.Length; i++) tot += xs[i];
      return tot / xs.Length;
    }


    /// <summary>
    /// Returns sum of an IList of floats.
    /// </summary>
    /// <param name="xs"></param>
    /// <returns></returns>
    public static float Sum(IList<float> xs)
    {
      double tot = 0;
      for (int i = 0; i < xs.Count; i++) tot += xs[i];
      return (float)tot;
    }

    public static double Min(IList<float> xs)
    {
      float min = float.MaxValue;
      for (int i = 0; i < xs.Count; i++)
        if (xs[i] < min)
          min = xs[i];
      return min;
    }


    /// <summary>
    /// Returns the maximum value in an array of floats.
    /// </summary>
    /// <param name="xs"></param>
    /// <returns></returns>
    public static double Max(float[] xs)
    {
      float max = float.MinValue;
      for (int i = 0; i < xs.Length; i++)
        if (xs[i] > max)
          max = xs[i];
      return max;
    }


    /// <summary>
    /// Returns the average value in an IList of floats.
    /// </summary>
    /// <param name="xs"></param>
    /// <returns></returns>
    public static double Average(IList<float> xs)
    {
      float tot = 0;
      for (int i = 0; i < xs.Count; i++) tot += xs[i];
      return tot / xs.Count;
    }


    /// <summary>
    /// Applies the Log transformation to values in an array, with a minimum value specified.
    /// </summary>
    /// <param name="vals"></param>
    /// <param name="minTruncationValue"></param>
    /// <returns></returns>
    public static float[] LogTruncatedWithMin(float[] vals, float minTruncationValue)
    {
      float[] ret = new float[vals.Length];
      for (int i = 0; i < vals.Length; i++)
      {
        if (vals[i] == 0)
          ret[i] = minTruncationValue;
        else
        {
          float logVal = (float)System.Math.Log(vals[i]);
          if (logVal < minTruncationValue)
            ret[i] = minTruncationValue;
          else
            ret[i] = logVal;
        }
      }
      return ret;
    }


    /// <summary>
    /// Returns the geometic average of an List of shorts.
    /// See: https://en.wikipedia.org/wiki/Geometric_mean
    /// </summary>
    /// <param name="xs"></param>
    /// <returns></returns>
    public static double AverageGeo(IList<short> xs)
    {
      // Compute in numerically stable way via logs
      double tot = 0.0;
      double totLogs = 0.0;
      double absTotLogs = 0.0;
      int numNonPositive = 0;
      for (int i = 0; i < xs.Count; i++)
      {
        tot += xs[i];
        totLogs += System.Math.Log(xs[i]);
        absTotLogs += System.Math.Log(System.Math.Abs(xs[i]));
        if (xs[i] <= 0) numNonPositive++;
      }

      //      Console.WriteLine(tot + " " + totLogs + " " + absTotLogs + " count:" + numNonPositive + " " + Math.Exp(absTotLogs / xs.Count));
      if (numNonPositive > 0)
        return System.Math.Pow(-1, (double)numNonPositive / (double)xs.Count) * System.Math.Exp(absTotLogs / xs.Count);
      else
        return System.Math.Exp(totLogs / xs.Count);
    }

    /// <summary>
    /// Returns the standard deviation of an IList of floats.
    /// </summary>
    /// <param name="vals"></param>
    /// <returns></returns>
    public static double StdDev(IList<float> vals)
    {
      double sum = 0;
      for (int i = 0; i < vals.Count; i++) sum += vals[i];
      double avg = sum / vals.Count;

      double ss = 0;
      for (int i = 0; i < vals.Count; i++) ss += (vals[i] - avg) * (vals[i] - avg);

      return System.Math.Sqrt(ss / vals.Count);
    }

    /// <summary>
    /// Returns the weighted average an array of doubles.
    /// </summary>
    /// <param name="d"></param>
    /// <param name="wts"></param>
    /// <returns></returns>
    public static double WtdAvg(double[] d, double[] wts)
    {
      double num = 0;
      double den = 0;
      for (int i = 0; i < d.Length; i++)
      {
        num += wts[i] * d[i];
        den += wts[i];
      }
      return num / den;
    }


    /// <summary>
    /// Returns the weighted average an array of floats.
    /// </summary>
    /// <param name="d"></param>
    /// <param name="wts"></param>
    /// <returns></returns>
    public static double WtdAvg(float[] d, double[] wts)
    {
      double num = 0;
      double den = 0;
      for (int i = 0; i < d.Length; i++)
      {
        num += wts[i] * d[i];
        den += wts[i];
      }
      return num / den;
    }


    /// <summary>
    /// Returns the weighted covariance of arrays of doubles.
    /// </summary>
    /// <param name="d1"></param>
    /// <param name="d2"></param>
    /// <param name="wts"></param>
    /// <returns></returns>
    public static double WtdCov(double[] d1, double[] d2, double[] wts)
    {
      double avg1 = WtdAvg(d1, wts);
      double avg2 = WtdAvg(d2, wts);
      double num = 0;
      double den = 0;
      for (int i = 0; i < d1.Length; i++)
      {
        num += wts[i] * (d1[i] - avg1) * (d2[i] - avg2);
        den += wts[i];
      }
      return num / den;
    }


    /// <summary>
    /// Returns the weighted covariance of arrays of floats.
    /// </summary>
    /// <param name="d1"></param>
    /// <param name="d2"></param>
    /// <param name="wts"></param>
    /// <returns></returns>
    public static double WtdCov(float[] d1, float[] d2, double[] wts)
    {
      double avg1 = WtdAvg(d1, wts);
      double avg2 = WtdAvg(d2, wts);
      double num = 0;
      double den = 0;
      for (int i = 0; i < d1.Length; i++)
      {
        num += wts[i] * (d1[i] - avg1) * (d2[i] - avg2);
        den += wts[i];
      }
      return num / den;
    }


    /// <summary>
    /// Returns the weighted correlation of arrays of doubles.
    /// </summary>
    /// <param name="d1"></param>
    /// <param name="d2"></param>
    /// <param name="wts"></param>
    /// <returns></returns>
    public static double CorrelationWeighted(double[] d1, double[] d2, double[] wts)
    {
      return WtdCov(d1, d2, wts) / (System.Math.Sqrt(WtdCov(d1, d1, wts) * WtdCov(d2, d2, wts)));
    }


    /// <summary>
    /// Returns the weighted correlation of arrays of floats.
    /// </summary>
    /// <param name="d1"></param>
    /// <param name="d2"></param>
    /// <param name="wts"></param>
    /// <returns></returns>
    public static double CorrelationWeighted(float[] d1, float[] d2, double[] wts)
    {
      return WtdCov(d1, d2, wts) / (System.Math.Sqrt(WtdCov(d1, d1, wts) * WtdCov(d2, d2, wts)));
    }


    /// <summary>
    /// Returns the correlation of two spans of doubles.
    /// </summary>
    /// <param name="xs"></param>
    /// <param name="ys"></param>
    /// <returns></returns>
    public static double Correlation(Span<double> xs, Span<double> ys)
    {
      //TODO: check here that arrays are not null, of the same length etc

      double sx = 0.0;
      double sy = 0.0;
      double sxx = 0.0;
      double syy = 0.0;
      double sxy = 0.0;

      int n = xs.Length;

      for (int i = 0; i < n; ++i)
      {
        double x = xs[i];
        double y = ys[i];

        sx += x;
        sy += y;
        sxx += x * x;
        syy += y * y;
        sxy += x * y;
      }

      // covariation
      double cov = sxy / n - sx * sy / n / n;

      // standard error of x
      double sigmax = System.Math.Sqrt(sxx / n - sx * sx / n / n);

      // standard error of y
      double sigmay = System.Math.Sqrt(syy / n - sy * sy / n / n);

      // correlation is just a normalized covariation
      return cov / sigmax / sigmay;
    }



    /// <summary>
    /// Returns the correlation of two Spans of FP16.
    /// </summary>
    /// <param name="xs"></param>
    /// <param name="ys"></param>
    /// <returns></returns>
    public static double Correlation(Span<FP16> xs, Span<FP16> ys)
    {
      //TODO: check here that arrays are not null, of the same length etc

      double sx = 0.0;
      double sy = 0.0;
      double sxx = 0.0;
      double syy = 0.0;
      double sxy = 0.0;

      int n = xs.Length;

      for (int i = 0; i < n; ++i)
      {
        double x = xs[i];
        double y = ys[i];

        sx += x;
        sy += y;
        sxx += x * x;
        syy += y * y;
        sxy += x * y;
      }

      // covariation
      double cov = sxy / n - sx * sy / n / n;

      // standard error of x
      double sigmax = System.Math.Sqrt(sxx / n - sx * sx / n / n);

      // standard error of y
      double sigmay = System.Math.Sqrt(syy / n - sy * sy / n / n);

      // correlation is just a normalized covariation
      return cov / sigmax / sigmay;
    }


    /// <summary>
    /// Returns the correlation of two Spans of floats.
    /// </summary>
    /// <param name="xs"></param>
    /// <param name="ys"></param>
    /// <returns></returns>
    public static double Correlation(Span<float> xs, Span<float> ys)
    {
      //TODO: check here that arrays are not null, of the same length etc

      double sx = 0.0;
      double sy = 0.0;
      double sxx = 0.0;
      double syy = 0.0;
      double sxy = 0.0;

      int n = xs.Length;

      for (int i = 0; i < n; ++i)
      {
        double x = xs[i];
        double y = ys[i];

        sx += x;
        sy += y;
        sxx += x * x;
        syy += y * y;
        sxy += x * y;
      }

      // covariation
      double cov = sxy / n - sx * sy / n / n;

      // standard error of x
      double sigmax = System.Math.Sqrt(sxx / n - sx * sx / n / n);

      // standard error of y
      double sigmay = System.Math.Sqrt(syy / n - sy * sy / n / n);

      // correlation is just a normalized covariation
      return cov / sigmax / sigmay;
    }
  }

}
