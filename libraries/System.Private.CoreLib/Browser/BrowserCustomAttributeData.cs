using System.Collections.Generic;
using System.Reflection;

namespace NetJs
{
    class BrowserCustomAttributeData : CustomAttributeData
    {
        ConstructorInfo _constructor;
        IList<CustomAttributeTypedArgument> _cArguments;
        IList<CustomAttributeNamedArgument> _nArguments;

        public BrowserCustomAttributeData(ConstructorInfo constructor, IList<CustomAttributeTypedArgument> cArguments, IList<CustomAttributeNamedArgument> nArguments)
        {
            _constructor = constructor;
            _cArguments = cArguments;
            _nArguments = nArguments;
        }

        public override ConstructorInfo Constructor => _constructor;
        public override IList<CustomAttributeTypedArgument> ConstructorArguments => _cArguments;
        public override IList<CustomAttributeNamedArgument> NamedArguments => _nArguments;
    }
}
