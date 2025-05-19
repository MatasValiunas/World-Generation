public static class FunctionValidator
{
    public static void ValidateFunctions(float[] functions, bool multiplication)
    {
        if (functions == null || functions.Length == 0)
            return;

        if (multiplication)
        {
            // If multiplication is selected, no value can be 0
            for (int i = 0; i < functions.Length; i++)
            {
                if (functions[i] == 0)
                {
                    functions[i] = 0.01f;
                }
            }
        }
        else
        {
            // If multiplication is not selected, check if all values are 0
            bool allZero = true;
            foreach (var value in functions)
            {
                if (value != 0)
                {
                    allZero = false;
                    break;
                }
            }

            // If all are zero, set the first one to a non-zero value
            if (allZero && functions.Length > 0)
            {
                functions[0] = 1f;
            }
        }
    }
}
