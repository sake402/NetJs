
(function (global) {
    let NetJs = global.NetJs = {
        $$: {
            "System": {}
        }
    };
    NetJs.$$.S = NetJs.$$.System; //make sure we have System namespace even if we shorten it to S
    //keep boot types in this array for retreiver by AppDomain when it starts
    let bootTypes = [];
    NetJs.$bts = bootTypes;
    NetJs.$exports = {};
    //expose some js methods directly
    NetJs.floor = window.Math.floor;
    NetJs.trunc = window.Math.trunc;

    NetJs.typesReady = false;
    const finalizer = new FinalizationRegistry((_object) => {
        _object.$dtor();
    });
    function isValueType(prototype) {
        var flags = prototype.$f;
        if (flags) {//value type
            return (flags & (1 << 9)) !== 0;
        }
        if (Object.hasOwn(prototype, "$bf")) {
            var flags = prototype.$bf();
            if (flags) {
                return (flags & (1 << 9)) !== 0;
            }
        }
        return null;
    }

    function isPrimitive(prototype) {
        var flags = prototype.$f;
        if (flags) {//ks primitives
            return (flags & (1 << 10)) !== 0;
        }
        return null;
    }

    NetJs.$finalizer = function (_this) {
        //TODO: nned a way to map heldValue back to the object being destroyed
        //finalizer.register(myObject, "_");
    }

    NetJs.getCallerName = function getCallerName() {
        const error = new Error();
        const stack = error.stack;

        if (!stack) return 'unknown';

        // Split stack by line breaks and clean empty lines
        const lines = stack.split('\n').filter(line => line.trim().length > 0);

        // Dynamic Indexing: Find where getCallerName itself sits in the stack array
        // This removes the fragile guesswork of lines[3] vs lines[2]
        const currentFuncIdx = lines.findIndex(line => line.includes('getCallerName'));

        // If we can't find this function, fallback safely
        if (currentFuncIdx === -1) return 'unknown';

        // The target caller is 2 steps further down the stack trace array
        // (Current Function -> Immediate Caller -> The Caller's Caller)
        const callerLine = lines[currentFuncIdx + 2];

        if (!callerLine) return 'top-level';

        // 1. Try V8 Engine Style (Chrome/Edge): " at CallerName (path...)"
        const chromeMatch = callerLine.match(/at\s+([^\s(]+)/);
        if (chromeMatch) {
            const tokens = chromeMatch[1].split('.');
            return tokens[tokens.length - 1];
        }

        // 2. Try SpiderMonkey Style (Firefox): "CallerName@path..."
        const firefoxMatch = callerLine.match(/^([^@]+)@/);
        if (firefoxMatch) {
            // If the method is a class/object method, it might look like "Object.method"
            const tokens = firefoxMatch[1].split('.');
            const name = tokens[tokens.length - 1];

            // Firefox marks anonymous functions with an empty string or <
            return (name && name !== '<') ? name : 'anonymous';
        }

        return 'anonymous';
    }


    NetJs.$nomix = class { }

    NetJs.$mix = function (...args) {
        var mixed = classes(...args);
        return mixed;
    }

    NetJs.$boot = function () {
        return NetJs;
    }

    NetJs.$bind = function (_function, _this) {
        var bound = _function.bind(_this);
        bound.$target = _this;
        return bound
    }

    NetJs.$ns = function (fullTypeName, type) {
        fullTypeName = fullTypeName.replaceAll("<", "$").replaceAll(",", "$").replaceAll(">", "$");
        type["$mfullName"] = fullTypeName;
        var namespace = NetJs;
        var builtNameSpace = "";
        var names = fullTypeName.split('.');
        if (fullTypeName.length > 0) {
            for (var i = 0; i < names.length - 1; i++) {
                var n = namespace[names[i]];
                if (builtNameSpace.length > 0)
                    builtNameSpace += ".";
                builtNameSpace += n;
                if (!n) {
                    n = {};
                    namespace[names[i]] = n;
                }
                namespace = n;
            }
        }
        var typeName = names[names.length - 1];
        let initialized = false;
        namespace[typeName] = type;
        // Object.defineProperty(namespace, typeName, {
        //     get: function () {
        //         if (NetJs.typesReady && !initialized) {
        //             if (type.$sinit)
        //                 type.$sinit();
        //             if (type.$cctor)
        //                 type.$cctor();
        //             initialized = true;
        //         }
        //         return type;
        //     }
        // });
        //namespace[typeName] = type;
        return type;
    }

    NetJs.$bt = function (fullTypeName, prototype) {
        var rprototype = NetJs.$ns(fullTypeName, prototype);
        bootTypes.push(prototype);
        prototype.$mfullName = fullTypeName;
        return rprototype;
    }

    NetJs.$cls = NetJs.$ns;

    NetJs.$dsp = function (lhs, T, getMethod, ...args) {
        var method = (lhs != null ? getMethod(lhs) : null) ?? getMethod(T);
        return method.apply(lhs, args);
        //return function () {
        //    return method.apply(lhs, arguments);
        //}
    }
    NetJs.$exp = function (fn) {
        return fn();
    }

    NetJs.$default = function (prototype) {
        if (!prototype)
            return null;
        if (prototype.$default)
            return prototype.$default();
        if (prototype.Zero !== undefined) //test long and decimal type
            return prototype.Zero;
        if (prototype.$is && prototype.$is(0, NetJs._)) {
            if (typeIsLong(prototype))
                return 0n;
            return 0;
        }
        // if (prototype.$is && prototype.$is(0n, NetJs._))
        // return 0n;
        // var model = prototype.$model;
        if (isValueType(prototype) === false) {
            return null;
        }
        //var asm = prototype.$asm;
        //if (asm && (prototype.$fullName || prototype.$fn)) {
        //    var model = asm.GetModel(prototype.$fullName ?? prototype.$fn);
        //    if (model.h != 0) {
        //        if ((model.fg & (1 << 9)) != 0) {//value type

        //        } else {
        //            return null;
        //        }
        //    }
        //}
        //if (Object.hasOwn(prototype, "$bf")) {
        //    var flags = prototype.$bf();
        //    if ((flags & (1 << 9)) != 0) { //value type
        //    } else {
        //        return null;
        //    }
        //}
        return new prototype();
    }
    NetJs.$box = function (value, prototype) {
        if (value == null)
            return null;
        if (value.$boxed)
            return value;
        let valueType = isValueType(prototype);
        let primitive = isPrimitive(prototype);
        if (valueType == false) {  //if reference type, no need boxing
            if (primitive == true) { //primitive reference type like string still need boxing. This is how we ensure their interfaces works
            } else
                return value;
        } else if (valueType == true) { //Nonprimitive value types are on the heap in JavaScript already
            if (prototype.name !== "Nullable$$") { //boxing a value into its nullable type, allow
                if (primitive == false) {
                    return value;
                }
            }
        }
        var instance = new prototype();
        if (prototype.name === "Nullable$$") { //boxing a value into its nullable type, allow
            instance.$ctor$2(value);
        } else {
            instance.m_value = value;
            instance.$boxed = true;
        }
        return instance;
    }

    NetJs.$unbox = function (value, valueType) {
        if (value === null)
            return value;
        if (!valueType || NetJs.$is(value, valueType)) {
            if (!value.$boxed)
                return value;
            return value.m_value;
        }
        throw new Error();
    }

    NetJs.$ifnn = function (value, whenNotNull) {
        var v = typeof value == "function" ? value() : value;
        if (v) {
            return whenNotNull(v);
        }
        return null;
    }

    NetJs.$discardRef = function () {
        let value;
        return {
            _: true,
            get $v() { return value; },
            set $v(v) { value = v; }
        }
    }
    NetJs._ = {
        get $v() { },
        set $v(v) { }
    }
    NetJs.$typeOf = function (prototype) {
        // let isClass = typeof prototype === 'function' && prototype.hasOwnProperty('prototype') && !prototype.hasOwnProperty('arguments');
        // let isProviderFunction = typeof prototype === 'function' && prototype.hasOwnProperty('arguments');
        // if (isProviderFunction)
        //     prototype = prototype();
        return prototype.$type ?? prototype;
    }
    NetJs.$sizeOf = function (prototype) {
        return prototype.$z;
    }
    NetJs.$firstOf = function (value, otherwise) {
        if (value !== null && value !== undefined)
            return value;
        if (typeof otherwise == 'function')
            return otherwise();
        return otherwise;
    }
    NetJs.$getType = function (value) {
        if (value == null)
            throw new Error();
        var prototype = Object.getPrototypeOf(value);
        if (prototype.$type)
            return prototype.$type;
        prototype = value.constructor;
        if (prototype.$type)
            return prototype.$type;
    }
    NetJs.$with = function (original, cloneFn) {
        var clone = original.Clone();
        cloneFn(clone);
        return clone;
    }
    NetJs.$is = function (value, type, outValue) {
        if (value === null || value === undefined)
            return false;
        let valueType = isValueType(type);
        let unboxedValue = value;
        if (value.$boxed && valueType) { //dont unbox if we are testing against ref type, needs to remain boxed as object
            unboxedValue = value.m_value;
        }

        let assigned = false;
        function assignOut() {
            if (!assigned && outValue) {
                if (value.$boxed && valueType) {
                    outValue.$v = value.m_value;
                }
                else
                    outValue.$v = value;
            }
        }
        const iOut = function (v) {
            if (v !== undefined) {
                if (outValue) {
                    if (value.$boxed) outValue.$v = value.m_value;
                    else outValue.$v = v;
                }
                assigned = true;
            }
        }
        if (type.$is && type.$is(value, iOut)) {
            assignOut();
            return true;
        }
        if (type.$fullName == "System.Object") {
            assignOut();
            return true;
        }
        if (value.constructor === type) {
            assignOut();
            return true;
        }
        if (value instanceof type) {
            assignOut();
            return true;
        }
        var interfaces = value.constructor?.$i;
        if (interfaces) {
            for (let i = 0; i < interfaces.length; i++) {
                if (interfaces[i] == type) {
                    assignOut();
                    return true;
                }
            }
        }
        //var prototype = Object.getPrototypeOf(value)?.$prototype
        //var typePrototype = type.$prototype;
        //if (prototype && typePrototype) {

        //}
        return false;
    }
    NetJs.$as = function (value, type) {
        var mvalue = value;
        var out = {
            $v: mvalue
            // set $v(v) {
            //     mvalue = v;
            // }
        }
        if (NetJs.$is(value, type, out)) {
            mvalue = out.$v;
            return mvalue;
        }
        return null;
    }
    NetJs.$nsh = function (left, op, right) {
        switch (op) {
            case "<<":
                return BigInt.asIntN(64, left << BigInt(right));
            case ">>":
                return left >> BigInt(right);
            case ">>>":
                return left >>> BigInt(right);
        }
    }
    function typeIsIntegerNumber(T) {
        var fn = T.$fullName;
        if (T.$k == 4) { //enum
            fn = T.$eut.$fullName;
        }
        return fn == "System.Byte" ||
            fn == "System.SByte" ||
            fn == "System.Char" ||
            fn == "System.Int16" ||
            fn == "System.UInt16" ||
            fn == "System.Int32" ||
            fn == "System.UInt32" ||
            fn == "System.IntPtr" ||
            fn == "System.UIntPtr";
    }
    function typeIsSignedIntegerNumber(T) {
        var fn = T.$fullName;
        if (T.$k == 4) { //enum
            fn = T.$eut.$fullName;
        }
        return fn == "System.SByte" ||
            fn == "System.Int16" ||
            fn == "System.Int32" ||
            fn == "System.IntPtr";
    }
    function typeIsFloatingNumber(T) {
        var fn = T.$fullName;
        if (T.$k == 4) { //enum
            fn = T.$eut.$fullName;
        }
        return fn == "System.Single" ||
            fn == "System.Double";
    }
    function typeIsLong(T) {
        var fn = T.$fullName;
        if (T.$k == 4) { //enum
            fn = T.$eut.$fullName;
        }
        return fn == "System.Int64" ||
            fn == "System.UInt64";
    }
    function tryCastNumeric(value, T) {
        var tvalue = typeof value;
        if (tvalue == "number" || tvalue == "bigint") {
            var toLong = typeIsLong(T);
            var toInteger = typeIsIntegerNumber(T);
            var toFloat = typeIsFloatingNumber(T);
            if (toInteger || toFloat || toLong) {
                // var min = T.MinValue;
                // var max = T.MaxValue;
                if (toLong) {
                    if (tvalue == "bigint") { //already bigint
                        return value;
                    }
                    return BigInt(Math.trunc(value));
                }
                else {
                    if (tvalue == "bigint") {
                        value = Number(value);
                    }
                    if (toInteger) {
                        var bitSize = NetJs.$sizeOf(T) * 8;
                        let allBitsSet;// T.System$Numerics$IBinaryNumber$$$AllBitsSet;
                        switch (bitSize) {
                            case 8:
                                allBitsSet = 0xFF;
                                break;
                            case 16:
                                allBitsSet = 0xFFFF;
                                break;
                            case 24:
                                allBitsSet = 0xFFFFFF;
                                break;
                            case 32:
                                allBitsSet = 0xFFFFFFFF;
                                break;
                        }
                        if (allBitsSet) {
                            value = value & allBitsSet;
                        } else {
                            value = value & max;
                        }
                        if (typeIsSignedIntegerNumber(T)) //cast to signed
                            value = (value << (32 - bitSize)) >> (32 - bitSize);
                        else // cast to unsigned
                            value = value >>> 0;
                    }
                }
                return value;
            }
        }
        return value;
    }
    NetJs.$wrap = function (value, signed) {
        let system = NetJs.$$.System ?? NetJs.$$.S;
        if (signed == 0) {
            if (value < 0 || value > 4294967295)
                return tryCastNumeric(value, system.UInt32);
        } else {
            if (value < -2147483648 || value > 2147483647)
                return tryCastNumeric(value, system.Int32);
        }
        return value;
    }

    NetJs.$ref = function (getter, setter, type) {
        //It is a common pattern to create a variable on the stack uninitialized and then pass the ref of such(via out or ref) to a method to provide the value
        //By default in js the variable are undefined.
        //If however the ref type is struct, it is possible the method being called try to access the properties of the uninitialized object on stack
        //Make sure the ref variable is initialized to default here
        //TODO: We probably should make the transpiler initialize an uinit variable on stack always to their default
        if (type) {
            var value = getter();
            if (value === undefined) {
                value = NetJs.$default(type);
                setter(value);
            }
        }
        return {
            get $v() { return getter(); },
            set $v(v) { setter(v); },
            $type: type
        }
    }
    NetJs.$cast = function (value, toType, originalType) {
        if (value === null)
            return null;
        var mvalue = value;
        var out = {
            $v: mvalue
            // set $v(v) {
            //     mvalue = v;
            // }
        }
        if (NetJs.$is(value, toType, out)) {
            mvalue = out.$v;
            return tryCastNumeric(mvalue, toType);
        }
        if (value instanceof NetJs.$typeRefOrPointer() && (typeIsIntegerNumber(toType) || typeIsLong(toType))) { //casting pointer to number
            var number = NetJs.castPtr2Address(value, toType);
            // if (number) {
            if (typeIsLong(toType))
                return BigInt(number);
            return number;
            // }
        }
        var tvalue = typeof (value);
        if ((tvalue == "number" || tvalue == "bigint") && value >= NetJs.$$.$interop.$vAddrOff && (toType.$fullName.startsWith("NetJs.Pointer<") || toType.$fullName.startsWith("NetJs.Ref<"))/*Object.getPrototypeListOf(type).contains(NetJs.$spc.System.IRefOrPointer)*/) { //casting number to pointer
            var ivalue = tvalue == "bigint" ? Number(value) : value;
            var pointer = NetJs.castAddress2Ptr(ivalue, toType);
            if (pointer)
                return pointer;
        }
        throw new Error();
    }
    NetJs.$tupleUnpack = function (fn) {
        return { set $v(tuple) { fn(tuple) } };
    }
    NetJs.$equals = function (a, b) {
        if (a === b)
            return true;
        return false;
    }
    function stringHashCode(str) {
        let hash = 0;
        if (str.length === 0) {
            return hash;
        }
        for (let i = 0; i < str.length; i++) {
            const char = str.charCodeAt(i);
            hash = ((hash << 5) - hash) + char; // Simple bitwise operation
            hash = hash & hash; // Convert to 32bit integer
        }
        return hash;
    }
    NetJs.$getHashCode = function (a) {
        if (typeof (a) == "number")
            return a;
        if (typeof (a) == "boolean")
            return a ? 1 : 0;
        if (a.ToString) {
            var str = a.ToString();
            return stringHashCode(str);
        }
        if (a.$type && a.$type.ToString) {
            var str = a.$type.ToString();
            return stringHashCode(str);
        }
        const jsonString = JSON.stringify(a);
        return stringHashCode(jsonString);
    }
    let inToString;
    NetJs.$toString = function (a, defaultValue) {
        if (defaultValue !== null && a === null)
            return defaultValue;
        if (!inToString) {
            if (a.ToString) {
                inToString = true;
                try {
                    var str = a.ToString();
                    return str;
                } finally {
                    inToString = false;
                }
            }
        }
        if (a.toString) {
            var str = a.toString();
            return str;
        }
        return null;
    }
    let inEquals;
    NetJs.$equals = function (a, b, T) {
        if (!inEquals) {
            if (T && a.System$IEquatable$$$Equals) {
                inEquals = true;
                try {
                    var eq = a.System$IEquatable$$$Equals(b);
                    return eq;
                } finally {
                    inEquals = false;
                }
            } else if (T && b.System$IEquatable$$$Equals) {
                inEquals = true;
                try {
                    var eq = b.System$IEquatable$$$Equals(a);
                    return eq;
                } finally {
                    inEquals = false;
                }
            }
            if (a.Equals) {
                inEquals = true;
                try {
                    var eq = a.Equals(b);
                    return eq;
                } finally {
                    inEquals = false;
                }
            } else if (b.Equals) {
                inEquals = true;
                try {
                    var eq = b.Equals(a);
                    return eq;
                } finally {
                    inEquals = false;
                }
            }
        }
        return a == b;
    }
    // NetJs.$destructure = function (tuple, ...refs) {
    //     var o = [];
    //     if (tuple.Deconstruct) {
    //         if (refs.length > 0)
    //             tuple.Deconstruct.apply(tuple, refs)
    //         else {
    //             o.length = 16;
    //             tuple.Deconstruct(
    //                 { set $v(v) { o[0] = v; } },
    //                 { set $v(v) { o[1] = v; } },
    //                 { set $v(v) { o[2] = v; } },
    //                 { set $v(v) { o[3] = v; } },
    //                 { set $v(v) { o[4] = v; } },
    //                 { set $v(v) { o[5] = v; } },
    //                 { set $v(v) { o[6] = v; } },
    //                 { set $v(v) { o[7] = v; } },
    //                 { set $v(v) { o[8] = v; } },
    //                 { set $v(v) { o[9] = v; } },
    //                 { set $v(v) { o[10] = v; } },
    //                 { set $v(v) { o[11] = v; } },
    //                 { set $v(v) { o[12] = v; } },
    //                 { set $v(v) { o[13] = v; } },
    //                 { set $v(v) { o[14] = v; } },
    //                 { set $v(v) { o[15] = v; } });
    //         }
    //     } else {
    //         for (let i = 1; ; i++) {
    //             var property = "Item" + i;
    //             var val = tuple[property];
    //             if (val !== undefined) {
    //                 if (refs.length > 0)
    //                     refs[i - 1].$v = val;
    //                 else
    //                     o.push(val);
    //             } else
    //                 break;
    //         }
    //     }
    //     return o;
    // }
    NetJs.$require = function () {
        //TODO:
        return [];
    }
})(window)
