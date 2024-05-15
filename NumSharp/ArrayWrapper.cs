using System.Data;

namespace NumSharp;

public class ArrayWrapper
{
    private double[] _array;
    private int[] _shape;

    public int[] Shape
    {
        get => _shape;
        private set
        {
            if (_array is null && value == default!) throw new InvalidConstraintException("Array and shape can't be undefined");
            _shape = 
                value == default! 
                    ? new[] { _array!.Length } 
                    : value;

            
            var shapeRes = 1;
            var autoIndex = -1;
            for (var i = 0; i < _shape.Length; i++)
            {
                if (_shape[i] == -1)
                {
                    if (_array is null || autoIndex != -1) throw new InvalidConstraintException("Can't create auto shape");
                    autoIndex = i;
                    continue;
                }

                shapeRes *= _shape[i];
            }

            if (_array is not null && _array.Length % shapeRes != 0)
                throw new InvalidConstraintException(
                    $"Can't set shape ({string.Join(", ", _shape)}) for {_array.Length}-length array");
            if (autoIndex != -1)
                _shape[autoIndex] = _array!.Length / shapeRes;
            _array ??= new double[shapeRes];
        }
    }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public ArrayWrapper(double[] arr, int[] shape = default!)
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    {
        _array = arr;
        Shape = shape;
    }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public ArrayWrapper(int[] shape)
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    {
        Shape = shape;
    }

    public int Length => _array.Length;
    
    public static implicit operator ArrayWrapper(double[] val)
        => new ArrayWrapper(val);

    public static implicit operator ArrayWrapper(double val)
        => new ArrayWrapper(new[] { val });

    public static implicit operator double(ArrayWrapper arr)
    {
        if (arr.Length != 1) throw new InvalidOperationException();
        return arr._array[0];
    }

    public static ArrayWrapper operator *(ArrayWrapper arr1, ArrayWrapper arr2)
        => DoOperator(arr1, arr2, (x, y) => x * y);

    public static ArrayWrapper operator /(ArrayWrapper arr1, ArrayWrapper arr2)
        => DoOperator(arr1, arr2, (x, y) => x / y);

    public static ArrayWrapper operator +(ArrayWrapper arr1, ArrayWrapper arr2)
        => DoOperator(arr1, arr2, (x, y) => x + y);

    public static ArrayWrapper operator -(ArrayWrapper arr1, ArrayWrapper arr2)
        => DoOperator(arr1, arr2, (x, y) => x - y);
    
    public ArrayWrapper Reshape(params int[] newShape)
    {
        var thisArrCopy = new double[Length];
        Array.Copy(_array, thisArrCopy, Length);
        var res = new ArrayWrapper(thisArrCopy, newShape);
        return res;
    }

    public ArrayWrapper Sum(params int[] axis)
    {
        var axisDimensions = Enumerable.Range(0, Shape.Length).Except(axis).ToArray();
        Array.Sort(axis);
        if (axis[^1] >= Shape.Length)
            throw new InvalidConstraintException($"Axis {axis[^1]} is greater then possible dimension");

        var resArr = new ArrayWrapper(axisDimensions.Select(x => Shape[x]).ToArray());
        var curIndex = Enumerable.Repeat(0, Shape.Length).ToArray();
        while (curIndex[axisDimensions[0]] < Shape[axisDimensions[0]])
        {
            var sum = 0.0;
            while (curIndex[axis[0]] < Shape[axis[0]])
            {
                sum += this[curIndex];

                curIndex[axis[^1]]++;
                for (var i = 1; i < axis.Length; i++)
                {
                    if (curIndex[axis[^i]] != Shape[axis[^i]]) break;
                    curIndex[axis[^(i + 1)]]++;
                    curIndex[axis[^i]] = 0;
                }
            }
            curIndex[axis[0]] = 0;

            resArr[GetIndex(axisDimensions.Select(x => curIndex[x]).ToArray())] = sum;

            curIndex[axisDimensions[^1]]++;
            for (var i = 1; i < axisDimensions.Length; i++)
            {
                if (curIndex[axisDimensions[^i]] != Shape[axisDimensions[^i]]) break;
                curIndex[axisDimensions[^(i + 1)]]++;
                curIndex[axisDimensions[^i]] = 0;
            }
        }

        return resArr;
    }

    public ArrayWrapper this[params int[] indexes] =>
        indexes.Length > Shape.Length
            ? throw new IndexOutOfRangeException()
            : indexes.Length == Shape.Length
                ? _array[GetIndex(indexes)]
                : new ArrayWrapper(_array[GetRange(indexes)], Shape[indexes.Length..]);

    private double this[int index]
    {
        get => _array[index];
        set => _array[index] = value;
    }

    private static ArrayWrapper DoOperator(ArrayWrapper arr1, ArrayWrapper arr2, Func<double, double, double> operation)
    {
        var resArr = new ArrayWrapper(BroadcastShape(arr1, arr2));

        for (var i = 0; i < resArr.Length; i++)
            resArr[i] = operation(arr1[GetIndexFromBroadcast(i, resArr.Shape, arr1.Shape)],
                arr2[GetIndexFromBroadcast(i, resArr.Shape, arr2.Shape)]);
        
        return resArr;
    }

    private static int[] GetIndexes(int[] shape, int index)
    {
        var res = new int[shape.Length];
        for (var i = res.Length - 1; i >= 0; i--)
        {
            res[i] = index % shape[i];
            index /= shape[i];
        }

        return res;
    }

    private static int GetIndexFromBroadcast(int broadcastIndex, int[] broadcastShape, int[] componentShape)
    {
        var stack = new Stack<int>();
        for (var i = 1; i <= componentShape.Length; i++)
        {
            stack.Push((broadcastIndex % broadcastShape[^i]) % componentShape[^i]);
            broadcastIndex /= broadcastShape[^i];
        }
        var res = 0;
        for (var i = 0; i < componentShape.Length; i++)
        {
            res = res * componentShape[i] + stack.Pop();
        }
        return res;
    }
    
    private static int[] BroadcastShape(ArrayWrapper arr1, ArrayWrapper arr2)
    {
        var p = Math.Max(arr1.Shape.Length, arr2.Shape.Length);
        var resShape = new int[p];
        for (var i = 1; i <= p; i++)
        {
            var aDimI = arr1.Shape.Length - i >= 0 ? arr1.Shape[^i] : 1;
            var bDimI = arr2.Shape.Length - i >= 0 ? arr2.Shape[^i] : 1;
            if (aDimI != 1 && bDimI != 1 && aDimI != bDimI)
                throw new InvalidConstraintException(
                    $"Couldn't broadcast ({string.Join(", ", arr1.Shape)}) and ({string.Join(", ", arr2.Shape)})");
            resShape[^i] = Math.Max(aDimI, bDimI);
        }

        return resShape;
    }

    private int GetIndex(params int[] indexes)
    {
        if (indexes.Length > Shape.Length) throw new IndexOutOfRangeException();
        var res = 0;
        for (var i = 0; i < indexes.Length; i++)
            res = res * Shape[i] + indexes[i];
        return res;
    }

    private Range GetRange(params int[] indexes)
    {
        var start = GetIndex(indexes);
        var end = start;
        for (var i = indexes.Length; i < Shape.Length; i++)
        {
            start *= Shape[i];
            end = end * Shape[i] + (Shape[i] - 1);
        }
        return new Range(start, end + 1);
    }
}