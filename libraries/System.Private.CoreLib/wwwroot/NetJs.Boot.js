// Polytype 0.17.0 – https://github.com/fasttime/Polytype
!function () { "use strict"; (t => { try { (class { })() } catch { return } throw Error("Polytype cannot be transpiled to ES5 or earlier code.") })(); const t = Function.prototype, e = Map, o = Object, { create: n, defineProperties: r, defineProperty: s, freeze: c, getOwnPropertyDescriptor: l, getOwnPropertyDescriptors: i, getPrototypeOf: u, setPrototypeOf: a } = o, p = Proxy, f = Reflect, { apply: y, construct: h, get: d, set: w } = f, g = Set, b = String, m = Symbol.hasInstance, v = TypeError, P = { apply(t, e, o) { if (ut(e)) { const [t] = o, n = mt(t) && tt(u(t)); if (n) { const r = G(t, n); e = new p(e, r), delete o[0] } } return k(e, ...o) } }, _ = { setPrototypeOf: () => !1 }, O = { __proto__: _, apply() { throw v("Constructor cannot be invoked without 'new'") } }, S = [], $ = c({ __proto__: null }), x = "result", j = "target", I = { apply(t, e, [o]) { if (mt(o)) { const t = M(e); if (yt(t, o)) return !0 } return !1 } }, q = ["function", "object", "undefined"], D = Symbol.for("Polytype inquiry: prototypes"), E = Symbol.for("Polytype inquiry: this supplier"); let L = t.call, T = t => L.bind(t); const k = T(t.bind), A = T(t[m]), C = T(t.toString), M = T(o.prototype.valueOf); T = null, L = null; const z = (t, e) => { if (t.has(e)) { const t = `Duplicate superclass ${vt(e)}`; throw v(t) } }, F = t => { if (!ut(t)) throw v("Argument is not a function") }, { classes: N } = { classes(...t) { if (!t.length) throw v("No superclasses specified"); const e = new g, o = new g; for (const n of t) { if (z(e, n), !at(n)) { const t = `${vt(n)} is not a constructor`; throw v(t) } const { prototype: t } = n; if (wt(t)) { const t = `Property 'prototype' of ${vt(n)} is not an object or null`; throw v(t) } e.add(n), null !== t && o.add(t) } const s = function (t, e) { const o = V(t), s = B(t), c = U(e), l = R(t), i = W(l, t, O), u = n(null, { constructor: Y(i, !0), class: Y(c) }), a = W(u, e, _), p = { class: Y(o), name: { get: s }, prototype: Y(a) }; return r(l, p), i }(e, o); return function (...t) { const e = new g, o = new g; for (const n of t) for (let t of n) for (; !e.has(t);) { e.add(t); { const { constructor: e } = t; at(e) && lt(e, o) } { const e = u(t); if (null === e) { it(t, "isPrototypeOf", I); break } t = e } } }(e, o), s } }, R = t => { const o = function (...o) { const n = [], s = function () { let t; return { get: () => t, set: e => { t = e } } }(); { const r = function (t, o) { const n = new e; let r, s; const c = t => { if (r === !t) throw v("Mixed argument styles"); r = t }; for (const e of o) { if (bt(e)) throw v("Invalid arguments"); let o, r; if (void 0 !== e && mt(o = e.super)) { if (c(!0), z(n, o), !t.has(o)) { const t = `${vt(o)} is not a direct superclass`; throw v(t) } if (r = e.arguments, bt(r)) { const t = `Invalid arguments for superclass ${vt(o)}`; throw v(t) } } else c(!1), s || (s = t.values()), o = s.next().value, r = e; const l = void 0 !== r ? H(r) : void 0; n.set(o, l) } return n }(t, o), l = function (t, e) { function o() { throw v("Operation not supported") } return delete o.length, delete o.name, o.prototype = J(t, e.prototype), a(o, e), c(o), o }(s.get, new.target); for (const e of t) { const t = r.get(e) ?? S, o = h(e, t, l), s = i(o); n.push(s) } } s.set(this); for (const t of n) r(this, t); for (let t; t = n.pop();)r(this, t) }; return a(o, null), o }, B = t => () => `(${[...t].map((({ name: t }) => b(t)))})`, G = (t, e) => ({ apply: (o, n, r) => (t = e() ?? t, y(o, t, r)) }), H = t.apply.bind(((...t) => t), null), J = (t, e) => { const o = n(e), r = { get: (e, o, n) => (t() || o === E && ht(n, s) && (n[x] = t), d(e, o, n)) }, s = new p(o, r); return s }, K = (t, e) => { const o = { get(o, r) { let s = d(t, r, e); if (ut(s)) { const t = Q(e, n); s = new p(s, t) } return s }, set: (o, n, r) => w(t, n, r, e) }, n = new p($, o); return n }, Q = (t, e) => ({ apply: (o, n, r) => (n === e && (n = t), y(o, n, r)) }), U = t => { const { class: e } = { class(e) { F(e); const { prototype: o } = e; if (!t.has(o)) { const t = mt(o) ? "Property 'prototype' of argument does not match any direct superclass" : "Property 'prototype' of argument is not an object"; throw v(t) } return K(o, this) } }; return e }, V = t => { const { class: e } = { class(e) { if (!t.has(e)) throw F(e), v("Argument is not a direct superclass"); return K(e, this) } }; return e }, W = (t, e, o) => { const n = [t, ...e], r = new p(t, { __proto__: o, get(t, o, s) { o === D && ht(s, r) && (s[x] = e.values()); const c = n.find(Pt(o)); if (void 0 !== c) return d(c, o, s) }, has: (t, e) => n.some(Pt(e)), set(t, e, o, r) { const s = n.find(Pt(e)); return void 0 !== s ? w(s, e, o, r) : (X(r, e, o, !0), !0) } }); return r }, X = (t, e, o, n = !1) => s(t, e, Y(o, !0, n)), Y = (t, e, o) => ({ value: t, writable: e, enumerable: o, configurable: e }), Z = t => { const e = ct(t, D); if (void 0 !== e) { const t = [...e]; for (const e of t) mt(e) || nt(); return t } }, tt = t => { const e = ct(t, E); return void 0 === e || ut(e) || nt(), e }, { getPrototypeListOf: et } = { getPrototypeListOf: t => { let e; { const o = u(t); null !== o ? (e = Z(o), e || (e = [o])) : e = [] } return e } }, ot = t => { let e = Z(t); if (!e) { const o = u(t); e = null !== o ? [o] : S } return e }, nt = () => { throw v("Corrupt inquiry result") }, { [m]: rt } = { [m](t) { st = !0; try { if (ut(this)) { const e = A(this, t); if (!st) return e; if (e || mt(t) && yt(this.prototype, t)) return !0 } return !1 } finally { st = !1 } } }; let st = !1; const ct = (t, e) => { const o = { __proto__: null, [j]: t }; return d(t, e, o), o[x] }, lt = (t, e) => { if (!e.has(t)) { e.add(t); const o = ot(t); let n = !1; for (const t of o) ft(t) ? it(t, "bind", P) : (lt(t, e), n = !0); n || X(t, m, rt) } }, it = (t, e, o) => { const n = l(t, e), r = n?.value; r && dt(r, e) && (n.value = new p(r, o), s(t, e, n)) }, ut = t => "function" == typeof t, at = t => { if (ut(t)) { const e = k(t); X(e, "prototype", null); const o = new p(e, pt); try { return new class extends o { }, !0 } catch { } } return !1 }, pt = { construct() { return this } }, ft = t => { if (dt(t, "")) { const e = l(t, m); if (e && !e.writable && !e.enumerable && !e.configurable && dt(e.value, "[Symbol.hasInstance]")) return !0 } return !1 }, yt = (t, e) => ot(e).some((e => e === t || yt(t, e))), ht = (t, e) => mt(t) && null === u(t) && t !== e && t[j] === e, dt = (t, e) => { let o; try { o = C(t) } catch { return !1 } const n = /^function (.*)\(\) {\s+\[native code]\s}$/.exec(o); return null != n && n[1] === e && !at(t) }, wt = t => void 0 === t || gt(t), gt = t => !q.includes(typeof t), bt = t => null === t || gt(t), mt = t => null !== t && !wt(t), vt = t => { let e; return ut(t) && (({ name: e } = t), null != e && (e = b(e), e)) || (e = b(t)), e }, Pt = t => e => t in e; !globalThis.hasOwnProperty("classes") && (X(globalThis, "classes", N), X(o, "getPrototypeListOf", et)) }();

(function (global) {
    let NetJs = global.NetJs = {};
    const GenericType0Placeholder = 20;
    // NetJs.$Ts = [];
    // for (let i = 0; i < 128; i++) { dont expect you to have up to 128 generic parameter on a class
    //     let name = "$T" + (i + 1);
    //     let cls;
    //     cls = class {
    //         static $metadata = { "h": (GenericType0Placeholder + i + 1) << 16 }
    //         static $is$T = i + 1;
    //         static $fullName = "";
    //         static get $type() { return cls; }
    //     }
    //     NetJs[name] = cls;
    //     NetJs.$Ts.push(cls);
    // }
    //expose some js methods directly
    NetJs.floor = window.Math.floor;
    NetJs.trunc = window.Math.trunc;

    NetJs.typesReady = false;
    const finalizer = new FinalizationRegistry((_object) => {
        _object.$dtor();
    });

    //NetJs.$asm = function (asmName, fn) {

    //}
    function getCoreAssembly() {
        let spcAssembly = NetJs.$spc.System.AppDomain.GlobalAssemblyRegistry["NetJs.System.Private.CoreLib"];
        return spcAssembly;
    }
    function isValueType(prototype) {
        var model = prototype.$metadata;
        if (model && model.fg) {//value type
            return (model.fg & (1 << 9)) !== 0;
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
        var model = prototype.$metadata;
        if (model && model.fg) {//ks primitives
            return (model.fg & (1 << 10)) !== 0;
        }
        if (Object.hasOwn(prototype, "$bf")) {
            var flags = prototype.$bf();
            if (flags) {
                return (flags & (1 << 10)) !== 0;
            }
        }
        return null;
    }

    NetJs.$finalizer = function (_this) {
        //TODO: nned a way to map heldValue back to the object being destroyed
        //finalizer.register(myObject, "_");
    }

    NetJs.getCallerName = function () {
        const error = new Error();
        const stack = error.stack;

        if (!stack) return 'unknown';

        // Split stack by line breaks
        const lines = stack.split('\n');

        // Line 0 is Error message, Line 1 is getCallerName itself
        // Line 2 is the actual function that called getCallerName
        // Line 3 is the actual function that called getCallerName's caller, which is what we want
        const callerLine = lines[3];

        if (!callerLine) return 'top-level';

        // Clean up V8 engine style traces: " at CallerName (path...)"
        const match = callerLine.match(/at\s+([^\s(]+)/);
        if (match) {
            var tokens = match[1].split('.');
            return tokens[tokens.length - 1];
        }
        return 'anonymous';
    }

    NetJs.$nomix = class { }

    NetJs.$mix = function () {
        var mixed = classes(...arguments);
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

    NetJs.$combine = function (_function1, _function2) {
        if (_function1 == null && _function2 == null)
            return null;
        if (_function1 != null && _function1.$functions && _function2 != null && _function2.$functions != null) {  //both multicast delegate
            _function1.$functions.push(..._function2.$functions);
            return _function1;
        }
        if (_function1 != null && _function1.$functions && _function2 != null) { //multicast delegate
            _function1.$functions.push(_function2);
            return _function1;
        }
        if (_function2 != null && _function2.$functions && _function1 != null) { //multicast delegate
            _function2.$functions.push(_function1);
            return _function2;
        }
        if (_function1 == null && _function2 != null)
            return _function2;
        if (_function2 == null && _function1 != null)
            return _function1;
        //both are single delegate
        var functions = [_function1, _function2];
        return { $functions: functions };
    }

    NetJs.$remove = function (functions, _function) {
        if (functions == null || _function == null)
            return functions;
        if (functions.$functions) {
            var index = -1;
            for (var i = functions.$functions.length - 1; i >= 0; i--) {
                var f = functions.$functions[i];
                if (f === _function) {
                    index = i;
                    break;
                }
            }
            if (index >= 0) {
                functions.$functions.splice(index, 1);
                if (functions.$functions.length == 1) {
                    return functions.$functions[0];
                }
            }
        } else {
            if (functions === _function)
                return null;
        }
        return functions;
    }

    NetJs.$ns = function (fullTypeName, type) {
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
        Object.defineProperty(namespace, typeName, {
            get: function () {
                if (NetJs.typesReady && !initialized) {
                    if (type.$sinit)
                        type.$sinit();
                    if (type.$cctor)
                        type.$cctor();
                    initialized = true;
                }
                return type;
            }
        });
        //namespace[typeName] = type;
        return type;
    }

    NetJs.$bt = function (fullTypeName, prototype) {
        var rprototype = NetJs.$ns(fullTypeName, prototype);
        //boot type will also have a type system
        let runtimeType;
        Object.defineProperty(rprototype, "$type", {
            configurable: true,
            get: function () {
                if (runtimeType)
                    return runtimeType;
                let spcAssembly = getCoreAssembly();
                var metadata = prototype.$metadata ?? { h: 0 };// spcAssembly.GetModel(fullTypeName);
                runtimeType = NetJs.$spc.System.RuntimeType.Create(spcAssembly, prototype, metadata, fullTypeName);
                runtimeType.$do_complete();
                return runtimeType;
            }
        });
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
        if (prototype.$is && prototype.$is(0, NetJs.$discardRef))
            return 0;
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
    NetJs.$ref = function (getter, setter, type) {
        return $.$spc.RefOrPointer$$(type)(getter, setter);
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
            if (primitive == false) {
                return value;
            }
        }
        var instance = new prototype(); //most valuetype link Int32 we want to box have a field called m_value 
        instance.m_value = value;
        instance.$boxed = true;
        return instance;
    }

    NetJs.$unbox = function (value, valueType) {
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

    NetJs.$discardRef = {
        get $v() { },
        set $v(v) { }
    }
    NetJs.$typeOf = function (prototype) {
        return prototype.$type ?? prototype;
    }
    NetJs.$sizeOf = function (prototype) {
        return prototype.$metadata?.sz;
    }
    NetJs.$firstOf = function (value, otherwise) {
        if (value)
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
        let assigned = false;
        function assignOut() {
            if (!assigned && outValue) {
                if (value.$boxed && isValueType(type)) {
                    outValue.$v = value.m_value;
                }
                else
                    outValue.$v = value;
            }
        }
        var iOut = {
            set $v(v) {
                if (v !== undefined) {
                    if (outValue) {
                        if (value.$boxed)
                            outValue.$v = value.m_value;
                        else
                            outValue.$v = v;
                    }
                    assigned = true;
                }
            }
        }
        if (type.$is && type.$is(value, iOut)) {
            assignOut();
            return true;
        }
        var prototype = type.prototype;
        if (prototype && prototype.$is && prototype.$is(value, iOut)) {
            assignOut();
            return true;
        }
        if (type.$fullName == "System.Object") {
            assignOut();
            return true;
        }
        if (value.$prototype && value.$prototype.$fullName == type.$fullName) {
            assignOut();
            return true;
        }
        if (value instanceof type) {
            assignOut();
            return true;
        }
        //var prototype = Object.getPrototypeOf(value)?.$prototype
        //var typePrototype = type.$prototype;
        //if (prototype && typePrototype) {

        //}
        return false;
    }
    NetJs.$as = function (value, type) {
        var mvalue = value;
        var out = { set $v(v) { mvalue = v; } }
        if (NetJs.$is(value, type, out)) {
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
    function typeIsFloatingNumber(T) {
        var fn = T.$fullName;
        return fn == "System.Float" ||
            fn == "System.Double";
    }
    function typeIsLong(T) {
        var fn = T.$fullName;
        return fn == "System.Int64" ||
            fn == "System.UInt64";
    }
    function tryCastNumeric(value, T) {
        var tvalue = typeof value;
        var toLong = typeIsLong(T);
        var toInteger = typeIsIntegerNumber(T);
        var toFloat = typeIsFloatingNumber(T);
        if ((tvalue == "number" || tvalue == "bigint") && (toInteger || toFloat || toLong)) {
            var min = T.MinValue;
            var max = T.MaxValue;
            //Detect long and ulong overflow, since JavaScript bitwise operation only work on 32 bit signed integer, we need to use BigInt to detect overflow, 
            //but we will still return a number
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
                    if (min < 0) //cast to signed
                        value = (value << (32 - bitSize)) >> (32 - bitSize);
                    else if (/*greaterThanZero || */min == 0) // cast to unsigned
                        value = value >>> 0;
                }
            }
            return value;
        }
        return value;
    }
    NetJs.$wrap = function (value, signed) {
        if (signed == 0) {
            if (value < 0 || value > 4294967295)
                return tryCastNumeric(value, NetJs.$spc.System.UInt32);
        } else {
            if (value < -2147483648 || value > 2147483647)
                return tryCastNumeric(value, NetJs.$spc.System.Int32);
        }
        return value;
    }
    //NetJs.$tryCast = function (value, type) {
    //    var mvalue = value;
    //    var out = { set $v(v) { mvalue = v; } }
    //    if (NetJs.$is(value, type, out))
    //        return NetJs.tryCastNumeric(mvalue, type);
    //    //if (type.$is && type.$is(value))
    //    //    return NetJs.tryCastNumeric(value);
    //    return null;
    //}

    //let virtualAddressSpaceSlotSize = 64 * 1024;
    //let virtualAddressOffset = 0x80000000;
    //let virtualAddressSpaces = [];

    //function freeAddressSpace(start, blocks) {
    //    while (blocks--) {
    //        virtualAddressSpaces[start] = undefined;
    //        start++;
    //    }
    //}

    //const pointerFinalizer = new FinalizationRegistry((startBlocks) => {
    //    freeAddressSpace(startBlock.start, startBlock.blocks);
    //});

    //function isFreeAddressSpace(n) {
    //    var v = virtualAddressSpaces[n];
    //    return v == undefined;
    //}
    //function getContaguousAddressSpace(blocks) {
    //    let start = 0;
    //    let nBlock = blocks;
    //    while (true) {
    //        if (isFreeAddressSpace(start)) {
    //            nBlock--;
    //        } else {
    //            nBlock = blocks;
    //        }
    //        start++;
    //        if (nBlock == 0)
    //            return start - blocks;
    //    }
    //}
    //function markAddressSpaceUsed(start, blocks, pointer) {
    //    pointerFinalizer.register(pointer, { start, blocks });
    //    while (blocks--) {
    //        virtualAddressSpaces[start] = pointer;
    //        start++;
    //        pointer = pointer.Add(virtualAddressSpaceSlotSize);
    //    }
    //}
    //function castPtr2Address(pointer) {
    //    if (pointer.$virtualAddress)
    //        return pointer.$virtualAddress;
    //    let array;
    //    let cur = pointer;
    //    let root = pointer;
    //    let offset = 0;
    //    while (cur) {
    //        root = cur;
    //        if (cur._arrayOffset) {
    //            offset += cur._arrayOffset;
    //        }
    //        if (cur._array) {
    //            array = cur._array;
    //            break;
    //        }
    //        if (cur._parentRef)
    //            cur = cur._parentRef;
    //    }
    //    if (array) {
    //        if (!root.$virtualAddress) {
    //            var len = array.length;
    //            var addressSpaces = Math.floor(((len - 1) / virtualAddressSpaceSlotSize) + 1);
    //            let freeAddressSpace = getContaguousAddressSpace(addressSpaces);
    //            markAddressSpaceUsed(freeAddressSpace, addressSpaces, root);
    //            root.$virtualAddress = virtualAddressOffset + freeAddressSpace;
    //        }
    //        return root.$virtualAddress + offset;
    //    }
    //    return null;
    //};
    //function castAddress2Ptr(address) {
    //    if (address < virtualAddressOffset) {
    //        throw new Error("Not a virtual address");
    //    }
    //    address -= virtualAddressOffset;
    //    var block = Math.floor(address / virtualAddressSpaceSlotSize);
    //    return virtualAddressSpaces[block];
    //}

    NetJs.$cast = function (value, toType, originalType) {
        if (value === null)
            return null;
        var mvalue = value;
        var out = { set $v(v) { mvalue = v; } }
        if (NetJs.$is(value, toType, out))
            return tryCastNumeric(mvalue, toType);
        if (value instanceof NetJs.$spc.System.IRefOrPointer && (typeIsIntegerNumber(toType) || typeIsLong(toType))) { //casting pointer to number
            var number = NetJs.castPtr2Address(value, toType);
            if (number) {
                if (typeIsLong(toType))
                    return BigInt(number);
                return number;
            }
        }
        var tvalue = typeof (value);
        if ((tvalue == "number" || tvalue == "bigint") && value >= NetJs.virtualAddressOffset && (toType.name == "Pointer$$" || toType.name == "Ref$$")/*Object.getPrototypeListOf(type).contains(NetJs.$spc.System.IRefOrPointer)*/) { //casting number to pointer
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
    NetJs.$typeArray = function (type) {
        if (!type)
            return NetJs.$spc.System.Array$$;
        return NetJs.$spc.System.Array$$(type);
    }
    NetJs.$typePointer = function (type) {
        if (!type)
            return NetJs.$spc.System.Pointer$$;
        return NetJs.$spc.System.Pointer$$(type);
    }
    NetJs.$array = function (type, length) {
        return NetJs.$spc.System.Array.$array(NetJs.$typeOf(type), length);
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
    NetJs.$destructure = function (tuple) {
        var o = [];
        if (tuple.Deconstruct) {
            o.length = 16;
            tuple.Deconstruct(
                { set $v(v) { o[0] = v; } },
                { set $v(v) { o[1] = v; } },
                { set $v(v) { o[2] = v; } },
                { set $v(v) { o[3] = v; } },
                { set $v(v) { o[4] = v; } },
                { set $v(v) { o[5] = v; } },
                { set $v(v) { o[6] = v; } },
                { set $v(v) { o[7] = v; } },
                { set $v(v) { o[8] = v; } },
                { set $v(v) { o[9] = v; } },
                { set $v(v) { o[10] = v; } },
                { set $v(v) { o[11] = v; } },
                { set $v(v) { o[12] = v; } },
                { set $v(v) { o[13] = v; } },
                { set $v(v) { o[14] = v; } },
                { set $v(v) { o[15] = v; } });
        } else {
            for (let i = 1; ; i++) {
                var property = "Item" + i;
                var val = tuple[property];
                if (val) {
                    o.push(val);
                } else
                    break;
            }
        }
        return o;
    }
})(window)
