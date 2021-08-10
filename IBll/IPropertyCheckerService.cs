using System;
using System.Collections.Generic;
using System.Text;

namespace IBll
{
    public interface IPropertyCheckerService
    {
        bool TypeHasProperties<T>(string fields);
    }
}
