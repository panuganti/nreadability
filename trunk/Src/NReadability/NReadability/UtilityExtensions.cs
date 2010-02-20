using System;

namespace NReadability
{
  internal static class UtilityExtensions
  {
    #region Public methods

    public static bool IsCloseToZero(this float x)
    {
      return Math.Abs(x) < float.Epsilon;
    }

    #endregion
  }
}
