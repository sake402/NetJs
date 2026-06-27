using NetJs;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

public class Number
{
    public T Cast<T>(object numberLike) where T : IMinMaxValue<T>
    {
        var min = T.MinValue;
        var max = T.MaxValue;
        numberLike = Script.Write<object>("numberLike & max");
        if (Script.Write<bool>("min === 0")) //if a non negative type
        {
            numberLike = Script.Write<object>("numberLike >>> 0");
        }
        return numberLike.As<T>();
    }
}
