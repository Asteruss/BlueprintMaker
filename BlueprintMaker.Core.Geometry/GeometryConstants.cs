using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlueprintMaker.Core.Geometry;

public static class GeometryConstants
{
    /// <summary>
    /// Допуск по умолчанию для сравнения чисел с плавающей запятой (в мм)
    /// </summary>
    public const double Tolerance = 0.0001;

    /// <summary>
    /// Дефолтная ширина бруса до момента материализации
    /// Нужна только для того, чтобы скелет и пустые пространства были видны на холсте
    /// </summary>
    public const double DisplayWidth = 10.0;
}
