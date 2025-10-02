var AsciinemaPlayer = (function (exports) {
  'use strict';

  function parseNpt(time) {
    if (typeof time === "number") {
      return time;
    } else if (typeof time === "string") {
      return time.split(":").reverse().map(parseFloat).reduce((sum, n, i) => sum + n * Math.pow(60, i));
    } else {
      return undefined;
    }
  }
  function debounce(f, delay) {
    let timeout;
    return function () {
      for (var _len = arguments.length, args = new Array(_len), _key = 0; _key < _len; _key++) {
        args[_key] = arguments[_key];
      }
      clearTimeout(timeout);
      timeout = setTimeout(() => f.apply(this, args), delay);
    };
  }
  function throttle(f, interval) {
    let enableCall = true;
    return function () {
      if (!enableCall) return;
      enableCall = false;
      for (var _len2 = arguments.length, args = new Array(_len2), _key2 = 0; _key2 < _len2; _key2++) {
        args[_key2] = arguments[_key2];
      }
      f.apply(this, args);
      setTimeout(() => enableCall = true, interval);
    };
  }

  class DummyLogger {
    log() {}
    debug() {}
    info() {}
    warn() {}
    error() {}
  }
  class PrefixedLogger {
    constructor(logger, prefix) {
      this.logger = logger;
      this.prefix = prefix;
    }
    log(message) {
      for (var _len = arguments.length, args = new Array(_len > 1 ? _len - 1 : 0), _key = 1; _key < _len; _key++) {
        args[_key - 1] = arguments[_key];
      }
      this.logger.log(`${this.prefix}${message}`, ...args);
    }
    debug(message) {
      for (var _len2 = arguments.length, args = new Array(_len2 > 1 ? _len2 - 1 : 0), _key2 = 1; _key2 < _len2; _key2++) {
        args[_key2 - 1] = arguments[_key2];
      }
      this.logger.debug(`${this.prefix}${message}`, ...args);
    }
    info(message) {
      for (var _len3 = arguments.length, args = new Array(_len3 > 1 ? _len3 - 1 : 0), _key3 = 1; _key3 < _len3; _key3++) {
        args[_key3 - 1] = arguments[_key3];
      }
      this.logger.info(`${this.prefix}${message}`, ...args);
    }
    warn(message) {
      for (var _len4 = arguments.length, args = new Array(_len4 > 1 ? _len4 - 1 : 0), _key4 = 1; _key4 < _len4; _key4++) {
        args[_key4 - 1] = arguments[_key4];
      }
      this.logger.warn(`${this.prefix}${message}`, ...args);
    }
    error(message) {
      for (var _len5 = arguments.length, args = new Array(_len5 > 1 ? _len5 - 1 : 0), _key5 = 1; _key5 < _len5; _key5++) {
        args[_key5 - 1] = arguments[_key5];
      }
      this.logger.error(`${this.prefix}${message}`, ...args);
    }
  }

  let wasm;
  const cachedTextDecoder = typeof TextDecoder !== 'undefined' ? new TextDecoder('utf-8', {
    ignoreBOM: true,
    fatal: true
  }) : {
    decode: () => {
      throw Error('TextDecoder not available');
    }
  };
  if (typeof TextDecoder !== 'undefined') {
    cachedTextDecoder.decode();
  }
  let cachedUint8Memory0 = null;
  function getUint8Memory0() {
    if (cachedUint8Memory0 === null || cachedUint8Memory0.byteLength === 0) {
      cachedUint8Memory0 = new Uint8Array(wasm.memory.buffer);
    }
    return cachedUint8Memory0;
  }
  function getStringFromWasm0(ptr, len) {
    ptr = ptr >>> 0;
    return cachedTextDecoder.decode(getUint8Memory0().subarray(ptr, ptr + len));
  }
  const heap = new Array(128).fill(undefined);
  heap.push(undefined, null, true, false);
  let heap_next = heap.length;
  function addHeapObject(obj) {
    if (heap_next === heap.length) heap.push(heap.length + 1);
    const idx = heap_next;
    heap_next = heap[idx];
    heap[idx] = obj;
    return idx;
  }
  function getObject(idx) {
    return heap[idx];
  }
  function dropObject(idx) {
    if (idx < 132) return;
    heap[idx] = heap_next;
    heap_next = idx;
  }
  function takeObject(idx) {
    const ret = getObject(idx);
    dropObject(idx);
    return ret;
  }
  function debugString(val) {
    // primitive types
    const type = typeof val;
    if (type == 'number' || type == 'boolean' || val == null) {
      return `${val}`;
    }
    if (type == 'string') {
      return `"${val}"`;
    }
    if (type == 'symbol') {
      const description = val.description;
      if (description == null) {
        return 'Symbol';
      } else {
        return `Symbol(${description})`;
      }
    }
    if (type == 'function') {
      const name = val.name;
      if (typeof name == 'string' && name.length > 0) {
        return `Function(${name})`;
      } else {
        return 'Function';
      }
    }
    // objects
    if (Array.isArray(val)) {
      const length = val.length;
      let debug = '[';
      if (length > 0) {
        debug += debugString(val[0]);
      }
      for (let i = 1; i < length; i++) {
        debug += ', ' + debugString(val[i]);
      }
      debug += ']';
      return debug;
    }
    // Test for built-in
    const builtInMatches = /\[object ([^\]]+)\]/.exec(toString.call(val));
    let className;
    if (builtInMatches.length > 1) {
      className = builtInMatches[1];
    } else {
      // Failed to match the standard '[object ClassName]'
      return toString.call(val);
    }
    if (className == 'Object') {
      // we're a user defined class or Object
      // JSON.stringify avoids problems with cycles, and is generally much
      // easier than looping through ownProperties of `val`.
      try {
        return 'Object(' + JSON.stringify(val) + ')';
      } catch (_) {
        return 'Object';
      }
    }
    // errors
    if (val instanceof Error) {
      return `${val.name}: ${val.message}\n${val.stack}`;
    }
    // TODO we could test for more things here, like `Set`s and `Map`s.
    return className;
  }
  let WASM_VECTOR_LEN = 0;
  const cachedTextEncoder = typeof TextEncoder !== 'undefined' ? new TextEncoder('utf-8') : {
    encode: () => {
      throw Error('TextEncoder not available');
    }
  };
  const encodeString = typeof cachedTextEncoder.encodeInto === 'function' ? function (arg, view) {
    return cachedTextEncoder.encodeInto(arg, view);
  } : function (arg, view) {
    const buf = cachedTextEncoder.encode(arg);
    view.set(buf);
    return {
      read: arg.length,
      written: buf.length
    };
  };
  function passStringToWasm0(arg, malloc, realloc) {
    if (realloc === undefined) {
      const buf = cachedTextEncoder.encode(arg);
      const ptr = malloc(buf.length, 1) >>> 0;
      getUint8Memory0().subarray(ptr, ptr + buf.length).set(buf);
      WASM_VECTOR_LEN = buf.length;
      return ptr;
    }
    let len = arg.length;
    let ptr = malloc(len, 1) >>> 0;
    const mem = getUint8Memory0();
    let offset = 0;
    for (; offset < len; offset++) {
      const code = arg.charCodeAt(offset);
      if (code > 0x7F) break;
      mem[ptr + offset] = code;
    }
    if (offset !== len) {
      if (offset !== 0) {
        arg = arg.slice(offset);
      }
      ptr = realloc(ptr, len, len = offset + arg.length * 3, 1) >>> 0;
      const view = getUint8Memory0().subarray(ptr + offset, ptr + len);
      const ret = encodeString(arg, view);
      offset += ret.written;
      ptr = realloc(ptr, len, offset, 1) >>> 0;
    }
    WASM_VECTOR_LEN = offset;
    return ptr;
  }
  let cachedInt32Memory0 = null;
  function getInt32Memory0() {
    if (cachedInt32Memory0 === null || cachedInt32Memory0.byteLength === 0) {
      cachedInt32Memory0 = new Int32Array(wasm.memory.buffer);
    }
    return cachedInt32Memory0;
  }
  /**
  * @param {number} cols
  * @param {number} rows
  * @param {number} scrollback_limit
  * @returns {Vt}
  */
  function create$1(cols, rows, scrollback_limit) {
    const ret = wasm.create(cols, rows, scrollback_limit);
    return Vt.__wrap(ret);
  }
  let cachedUint32Memory0 = null;
  function getUint32Memory0() {
    if (cachedUint32Memory0 === null || cachedUint32Memory0.byteLength === 0) {
      cachedUint32Memory0 = new Uint32Array(wasm.memory.buffer);
    }
    return cachedUint32Memory0;
  }
  function getArrayU32FromWasm0(ptr, len) {
    ptr = ptr >>> 0;
    return getUint32Memory0().subarray(ptr / 4, ptr / 4 + len);
  }
  const VtFinalization = typeof FinalizationRegistry === 'undefined' ? {
    register: () => {},
    unregister: () => {}
  } : new FinalizationRegistry(ptr => wasm.__wbg_vt_free(ptr >>> 0));
  /**
  */
  class Vt {
    static __wrap(ptr) {
      ptr = ptr >>> 0;
      const obj = Object.create(Vt.prototype);
      obj.__wbg_ptr = ptr;
      VtFinalization.register(obj, obj.__wbg_ptr, obj);
      return obj;
    }
    __destroy_into_raw() {
      const ptr = this.__wbg_ptr;
      this.__wbg_ptr = 0;
      VtFinalization.unregister(this);
      return ptr;
    }
    free() {
      const ptr = this.__destroy_into_raw();
      wasm.__wbg_vt_free(ptr);
    }
    /**
    * @param {string} s
    * @returns {any}
    */
    feed(s) {
      const ptr0 = passStringToWasm0(s, wasm.__wbindgen_malloc, wasm.__wbindgen_realloc);
      const len0 = WASM_VECTOR_LEN;
      const ret = wasm.vt_feed(this.__wbg_ptr, ptr0, len0);
      return takeObject(ret);
    }
    /**
    * @param {number} cols
    * @param {number} rows
    * @returns {any}
    */
    resize(cols, rows) {
      const ret = wasm.vt_resize(this.__wbg_ptr, cols, rows);
      return takeObject(ret);
    }
    /**
    * @returns {string}
    */
    inspect() {
      let deferred1_0;
      let deferred1_1;
      try {
        const retptr = wasm.__wbindgen_add_to_stack_pointer(-16);
        wasm.vt_inspect(retptr, this.__wbg_ptr);
        var r0 = getInt32Memory0()[retptr / 4 + 0];
        var r1 = getInt32Memory0()[retptr / 4 + 1];
        deferred1_0 = r0;
        deferred1_1 = r1;
        return getStringFromWasm0(r0, r1);
      } finally {
        wasm.__wbindgen_add_to_stack_pointer(16);
        wasm.__wbindgen_free(deferred1_0, deferred1_1, 1);
      }
    }
    /**
    * @returns {Uint32Array}
    */
    getSize() {
      try {
        const retptr = wasm.__wbindgen_add_to_stack_pointer(-16);
        wasm.vt_getSize(retptr, this.__wbg_ptr);
        var r0 = getInt32Memory0()[retptr / 4 + 0];
        var r1 = getInt32Memory0()[retptr / 4 + 1];
        var v1 = getArrayU32FromWasm0(r0, r1).slice();
        wasm.__wbindgen_free(r0, r1 * 4, 4);
        return v1;
      } finally {
        wasm.__wbindgen_add_to_stack_pointer(16);
      }
    }
    /**
    * @param {number} n
    * @returns {any}
    */
    getLine(n) {
      const ret = wasm.vt_getLine(this.__wbg_ptr, n);
      return takeObject(ret);
    }
    /**
    * @returns {any}
    */
    getCursor() {
      const ret = wasm.vt_getCursor(this.__wbg_ptr);
      return takeObject(ret);
    }
  }
  async function __wbg_load(module, imports) {
    if (typeof Response === 'function' && module instanceof Response) {
      if (typeof WebAssembly.instantiateStreaming === 'function') {
        try {
          return await WebAssembly.instantiateStreaming(module, imports);
        } catch (e) {
          if (module.headers.get('Content-Type') != 'application/wasm') {
            console.warn("`WebAssembly.instantiateStreaming` failed because your server does not serve wasm with `application/wasm` MIME type. Falling back to `WebAssembly.instantiate` which is slower. Original error:\n", e);
          } else {
            throw e;
          }
        }
      }
      const bytes = await module.arrayBuffer();
      return await WebAssembly.instantiate(bytes, imports);
    } else {
      const instance = await WebAssembly.instantiate(module, imports);
      if (instance instanceof WebAssembly.Instance) {
        return {
          instance,
          module
        };
      } else {
        return instance;
      }
    }
  }
  function __wbg_get_imports() {
    const imports = {};
    imports.wbg = {};
    imports.wbg.__wbindgen_error_new = function (arg0, arg1) {
      const ret = new Error(getStringFromWasm0(arg0, arg1));
      return addHeapObject(ret);
    };
    imports.wbg.__wbindgen_object_drop_ref = function (arg0) {
      takeObject(arg0);
    };
    imports.wbg.__wbindgen_object_clone_ref = function (arg0) {
      const ret = getObject(arg0);
      return addHeapObject(ret);
    };
    imports.wbg.__wbindgen_number_new = function (arg0) {
      const ret = arg0;
      return addHeapObject(ret);
    };
    imports.wbg.__wbindgen_bigint_from_u64 = function (arg0) {
      const ret = BigInt.asUintN(64, arg0);
      return addHeapObject(ret);
    };
    imports.wbg.__wbindgen_string_new = function (arg0, arg1) {
      const ret = getStringFromWasm0(arg0, arg1);
      return addHeapObject(ret);
    };
    imports.wbg.__wbg_set_f975102236d3c502 = function (arg0, arg1, arg2) {
      getObject(arg0)[takeObject(arg1)] = takeObject(arg2);
    };
    imports.wbg.__wbg_new_b525de17f44a8943 = function () {
      const ret = new Array();
      return addHeapObject(ret);
    };
    imports.wbg.__wbg_new_f841cc6f2098f4b5 = function () {
      const ret = new Map();
      return addHeapObject(ret);
    };
    imports.wbg.__wbg_new_f9876326328f45ed = function () {
      const ret = new Object();
      return addHeapObject(ret);
    };
    imports.wbg.__wbindgen_is_string = function (arg0) {
      const ret = typeof getObject(arg0) === 'string';
      return ret;
    };
    imports.wbg.__wbg_set_17224bc548dd1d7b = function (arg0, arg1, arg2) {
      getObject(arg0)[arg1 >>> 0] = takeObject(arg2);
    };
    imports.wbg.__wbg_set_388c4c6422704173 = function (arg0, arg1, arg2) {
      const ret = getObject(arg0).set(getObject(arg1), getObject(arg2));
      return addHeapObject(ret);
    };
    imports.wbg.__wbindgen_debug_string = function (arg0, arg1) {
      const ret = debugString(getObject(arg1));
      const ptr1 = passStringToWasm0(ret, wasm.__wbindgen_malloc, wasm.__wbindgen_realloc);
      const len1 = WASM_VECTOR_LEN;
      getInt32Memory0()[arg0 / 4 + 1] = len1;
      getInt32Memory0()[arg0 / 4 + 0] = ptr1;
    };
    imports.wbg.__wbindgen_throw = function (arg0, arg1) {
      throw new Error(getStringFromWasm0(arg0, arg1));
    };
    return imports;
  }
  function __wbg_finalize_init(instance, module) {
    wasm = instance.exports;
    __wbg_init.__wbindgen_wasm_module = module;
    cachedInt32Memory0 = null;
    cachedUint32Memory0 = null;
    cachedUint8Memory0 = null;
    return wasm;
  }
  function initSync(module) {
    if (wasm !== undefined) return wasm;
    const imports = __wbg_get_imports();
    if (!(module instanceof WebAssembly.Module)) {
      module = new WebAssembly.Module(module);
    }
    const instance = new WebAssembly.Instance(module, imports);
    return __wbg_finalize_init(instance, module);
  }
  async function __wbg_init(input) {
    if (wasm !== undefined) return wasm;
    const imports = __wbg_get_imports();
    if (typeof input === 'string' || typeof Request === 'function' && input instanceof Request || typeof URL === 'function' && input instanceof URL) {
      input = fetch(input);
    }
    const {
      instance,
      module
    } = await __wbg_load(await input, imports);
    return __wbg_finalize_init(instance, module);
  }

  var exports$1 = /*#__PURE__*/Object.freeze({
      __proto__: null,
      Vt: Vt,
      create: create$1,
      default: __wbg_init,
      initSync: initSync
  });

  const base64codes = [62,0,0,0,63,52,53,54,55,56,57,58,59,60,61,0,0,0,0,0,0,0,0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20,21,22,23,24,25,0,0,0,0,0,0,26,27,28,29,30,31,32,33,34,35,36,37,38,39,40,41,42,43,44,45,46,47,48,49,50,51];

          function getBase64Code(charCode) {
              return base64codes[charCode - 43];
          }

          function base64_decode(str) {
              let missingOctets = str.endsWith("==") ? 2 : str.endsWith("=") ? 1 : 0;
              let n = str.length;
              let result = new Uint8Array(3 * (n / 4));
              let buffer;

              for (let i = 0, j = 0; i < n; i += 4, j += 3) {
                  buffer =
                      getBase64Code(str.charCodeAt(i)) << 18 |
                      getBase64Code(str.charCodeAt(i + 1)) << 12 |
                      getBase64Code(str.charCodeAt(i + 2)) << 6 |
                      getBase64Code(str.charCodeAt(i + 3));
                  result[j] = buffer >> 16;
                  result[j + 1] = (buffer >> 8) & 0xFF;
                  result[j + 2] = buffer & 0xFF;
              }

              return result.subarray(0, result.length - missingOctets);
          }

          const wasm_code = base64_decode("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ACADKQAAIgk3AAggByAEQRRsaiICQqCAgIAQNwIAIAIgCTcACCAAQRBqIANBCGovAAAiADsAACACQRBqIAA7AAAgAUEBaiIAIAVPDQogByAAQRRsaiIAKAIEQQJGBEAgAUECaiIBIAVPDQ0gByABQRRsaiIBQqCAgIAQNwIAIAEgAykAADcACCABQRBqIANBCGovAAA7AAALIABCIDcCACAAQQhqIQRBAiEGDBALIAFBAWoiACAFTw0KIAcgAEEUbGoiAEKggICAEDcCACAAQQhqIQRBACEGDA8LIAEgBUG4ocAAEGwACyAEQYACQfCdwAAQbAALIARBFUGAnsAAEGwACyAGQbQBQZCewAAQbAALIAAgBUHYocAAEGwACyAAIAVByKHAABBsAAsgASAFQeihwAAQbAALIAEgBUGIosAAEGwACyAEIAVBuKLAABBsAAsgACAFQciiwAAQbAALIAAgBUGoosAAEGwACyABIAVB2KLAABBsAAsgBSABQQFrIgFLBEBBASEGIABBATYCBCAAIAI2AgAgByABQRRsaiIBQqCAgIAQNwIAIAEgAykAADcACCABQRBqIANBCGovAAA7AAAgAEEIaiEEDAMLIAEgBUGYosAAEGwACyAAQQE2AgQgACACNgIAIAggAykAADcAACAIQQhqIANBCGovAAA7AAAgASAFSQRAIAcgAUEUbGoiAEKggICAEDcCACAAQQhqIQRBASEGDAILIAEgBUH4ocAAEGwACyAAQQE2AgQgACACNgIAIABBCGohBEEBIQYLIAQgAykAADcAACAEQQhqIANBCGovAAA7AAALIAYLyAgCCX8BfiMAQUBqIgUkAAJAA0AgASgCDEEUayEDIAEoAhAhBAJAAkADQCADQRRqIgIgBEYNASABIANBKGo2AgwgA0EYaiACIQMoAgAiCUUNAAsgASgCCCIGDQEgBUEYaiICQRBqIgQgA0EQaigCADYCACACQQhqIgYgA0EIaikCADcDACAFIAMpAgA3AxhBACEDIAEoAgBFBEAgAUEAEIQBIAEoAgghAwsgASgCBCADQRRsaiICIAUpAxg3AgAgAkEQaiAEKAIANgIAIAJBCGogBikDADcCACABIAEoAghBAWo2AggMAgsgASgCCARAIAEpAgAhCyABQoCAgIDAADcCACAAIAs3AgAgAUEIaiIBKAIAIQIgAUEANgIAIABBCGogAjYCAAwDCyAAQYCAgIB4NgIADAILIANBCGoiCi0AACECAkACQCABKAIEIgggBkEUbGpBFGsiBC0ACCIHQQJGBEAgAkECRw0CDAELIAJBAkYNASACIAdHDQEgB0UEQCAELQAJIANBCWotAABGDQEMAgsgBC0ACSADQQlqLQAARw0BIAQtAAogA0EKai0AAEcNASAELQALIANBC2otAABHDQELIANBDGotAAAhAgJAIAQtAAwiB0ECRgRAIAJBAkcNAgwBCyACQQJGDQEgAiAHRw0BIAdFBEAgBC0ADSADQQ1qLQAARw0CDAELIAQtAA0gA0ENai0AAEcNASAELQAOIANBDmotAABHDQEgBC0ADyADQQ9qLQAARw0BCyAELQAQIANBEGoiBy0AAEcNACAELQARIANBEWotAABHDQAgBCgCACICQfz//wBxQbDBA0YNACACQeD//wBxQYDLAEYNACACQYD+/wBxQYDQAEYNACAEKAIEQQFLDQAgAkGA//8AcUGAygBGDQAgAygCACICQfz//wBxQbDBA0YNACACQeD//wBxQYDLAEYNACACQYD+/wBxQYDQAEYNACAJQQFHDQAgAkGA//8AcUGAygBGDQAgBUEYaiICQRBqIgQgBygCADYCACACQQhqIgcgCikCADcDACAFIAMpAgA3AxggASgCACAGRgR/IAEgBhCEASABKAIEIQggASgCCAUgBgtBFGwgCGoiAiAFKQMYNwIAIAJBEGogBCgCADYCACACQQhqIAcpAwA3AgAgASABKAIIQQFqNgIIDAELCyABKQIAIQsgAUKAgICAwAA3AgAgBUEQaiIEIAFBCGoiAigCADYCACACQQA2AgAgBSALNwMIIAVBGGoiBkEQaiIIIANBEGooAgA2AgAgBkEIaiIGIANBCGopAgA3AwAgBSADKQIANwMYIAFBABCEASABKAIEIAIoAgBBFGxqIgEgBSkDGDcCACABQRBqIAgoAgA2AgAgAUEIaiAGKQMANwIAIAIgAigCAEEBajYCACAAQQhqIAQoAgA2AgAgACAFKQMINwIACyAFQUBrJAALswcCCH8BfiMAQUBqIgUkAAJAA0AgASgCDEEUayECIAEoAhAhBAJAAkADQCACQRRqIgMgBEYNASABIAJBKGo2AgwgAkEYaiADIQIoAgBF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aiAFQQhqKQIANwIADAILIAhBgICAgHg2AgAgBSgCACICRQ0BIAUoAgQgAkEUbEEEEOUBDAELIANBAEGQpMAAEGwACyAFQSBqJAAgAUEIaiAIQQhqKQIANwIAIAEgBikCKDcCACAAQQhqIAdBCGopAgA3AgAgACAGKQIINwIADAQLIAAgBikCCDcCACAAQQhqIAZBEGopAgA3AgAMAwsCQCACIAxHBEAgASACQRBqIgo2AhAgAigCACIEQYCAgIB4Rw0BCyAGQQA7ATggBkECOgA0IAZBAjoAMCAGQqCAgIAQNwIoIAYgCSADazYCPCAGQQhqIgEgBkEoahAwIAAgBikCCDcCACAGQQA6ABQgAEEIaiABQQhqKQIANwIADAMLIA8gAikCBDcCACAQIAJBDGooAgA2AgAgBiAENgIYIAZBKGohByAGQRhqIQMjAEEgayICJAACQCAJIAZBCGoiBSgCCCIEayIIRQRAIAdBAToAACAHIAMpAgA3AgQgB0EMaiADQQhqKQIANwIADAELIAUtAAxFBEAgAiAINgIcIAJBADsBGCACQQI6ABQgAkECOgAQIAJCoICAgBA3AgggBSACQQhqEDAgB0EBOgAAIAcgAykCADcCBCAHQQxqIANBCGopAgA3AgAMAQsgAy0ADEUEQCADEGILIAMoAgQhCyADKAIIIg0gCE0EQCAFIAsgCyANQRRsahB6QQAhCAJAIAMtAAwNACAFQQA6AAxBASEIIAUoAggiBCAJTw0AIAJBADsBGCACQQI6ABQgAkECOgAQIAJCoICAgBA3AgggAiAJIARrNgIcIAUgAkEIahAwCyAHQYCAgIB4NgIEIAcgCDoAACADKAIAIgNFDQEgCyADQRRsQQQQ5QEMAQsCQCALIAhBFGxqKAIERQRAIARBAWshDSAERQ0BIAJBEGoiESAFKAIEIgsgDUEUbGoiDUEQai8AADsBACACIA0pAAg3AwggBSgCACAERgRAIAUgBBCEASAFKAIEIQsgBSgCCCEECyAEQRRsIAtqIgQgAikDCDcCCCAEQqCAgIAQNwIAIARBEGogES8BADsBACAFIAUoAghBAWo2AgggCEEBayEICwJAIAMoAggiBCAITwRAIAMoAgQhBCACIAg2AgQgAiAENgIADAELIAggBEGApMAAEOsBAAsgBSACKAIAIgQgBCACKAIEQRRsahB6IAMoAgAhBSADKAIEIgsgAygCCCIEIAgQswEgByALNgIIIAcgBTYCBCAHQQE6AAAgByADLQAMOgAQIAcgBCAEIAhrIgMgAyAESxs2AgwMAQsgDUEAQfCjwAAQbAALIAJBIGokACAGLQAoRQRAIAEgBikCCDcCACABQQhqIAZBEGopAgA3AgAgBigCLCICQYCAgIB4Rg0BIAJFDQEgBigCMCACQRRsQQQQ5QEMAQsLIAYoAixBgICAgHhHBEAgASAOKQIANwIAIAFBCGogDkEIaikCADcCAAsgACAGKQIINwIAIABBCGogBkEQaikCADcCAAwBCyAAQYCAgIB4NgIAIAFBgICAgHg2AgALIAZBQGskAAvrBAEPfyMAQTBrIQMCQCAARQ0AIAJFDQAgA0EQaiIGIAEgAEFsbGoiDCIHQRBqKAIANgIAIANBCGoiCCAHQQhqKQIANwMAIAMgBykCADcDACACQRRsIQkgAiIEIQoDQCAMIARBFGxqIQUDQCADQRhqIgFBEGoiDSAGKAIANgIAIAFBCGoiDiAIKQMANwMAIAMgAykDADcDGEEAIQEDQCABIAVqIgsoAgAhDyALIANBGGogAWoiCygCADYCACALIA82AgAgAUEEaiIBQRRHDQALIAYgDSgCADYCACAIIA4pAwA3AwAgAyADKQMYNwMAIAAgBEsEQCAFIAlqIQUgAiAEaiEEDAELCyAEIABrIgQEQCAEIAogBCAKSRshCgwBBSAHIAMpAwA3AgAgB0EQaiADQRBqIgYoAgA2AgAgB0EIaiADQQhqIggpAwA3AgAgCkECSQ0CQQEhBQNAIAYgByAFQRRsaiIJQRBqIgwoAgA2AgAgCCAJQQhqIg0pAgA3AwAgAyAJKQIANwMAIAIgBWohBANAIANBGGoiAUEQaiIOIAYoAgA2AgAgAUEIaiILIAgpAwA3AwAgAyADKQMANwMYIAcgBEEUbGohD0EAIQEDQCABIA9qIhAoAgAhESAQIANBGGogAWoiECgCADYCACAQIBE2AgAgAUEEaiIBQRRHDQALIAYgDigCADYCACAIIAspAwA3AwAgAyADKQMYNwMAIAAgBEsEQCACIARqIQQMAQsgBCAAayIEIAVHDQALIAkgAykDADcCACAMIAYoAgA2AgAgDSAIKQMANwIAIAogBUEBaiIFRw0ACwsLCwv8BAEKfyMAQTBrIgMkACADQQM6ACwgA0EgNgIcIANBADYCKCADIAE2AiQgAyAANgIgIANBADYCFCADQQA2AgwCfwJAAkACQCACKAIQIgpFBEAgAigCDCIARQ0BIAIoAgghASAAQQN0IQUgAEEBa0H/////AXFBAWohByACKAIAIQADQCAAQQRqKAIAIgQEQCADKAIgIAAoAgAgBCADKAIkKAIMEQEADQQLIAEoAgAgA0EMaiABKAIEEQAADQMgAUEIaiEBIABBCGohACAFQQhrIgUNAAsMAQsgAigCFCIARQ0AIABBBXQhCyAAQQFrQf///z9xQQFqIQcgAigCCCEIIAIoAgAhAANAIABBBGooAgAiAQRAIAMoAiAgACgCACABIAMoAiQoAgwRAQANAwsgAyAFIApqIgFBEGooAgA2AhwgAyABQRxqLQAAOgAsIAMgAUEYaigCADYCKCABQQxqKAIAIQRBACEJQQAhBgJAAkACQCABQQhqKAIAQQFrDgIAAgELIAggBEEDdGoiDCgCBEHzAEcNASAMKAIAKAIAIQQLQQEhBgsgAyAENgIQIAMgBjYCDCABQQRqKAIAIQQCQAJAAkAgASgCAEEBaw4CAAIBCyAIIARBA3RqIgYoAgRB8wBHDQEgBigCACgCACEEC0EBIQkLIAMgBDYCGCADIAk2AhQgCCABQRRqKAIAQQN0aiIBKAIAIANBDGogASgCBBEAAA0CIABBCGohACALIAVBIGoiBUcNAAsLIAcgAigCBE8NASADKAIgIAIoAgAgB0EDdGoiACgCACAAKAIEIAMoAiQoAgwRAQBFDQELQQEMAQtBAAsgA0EwaiQAC+8EAQV/IwBBEGsiAyQAAkACQAJAAkACQAJAAkAgACgCpAEiAkEBTQRAAkAgAUH/AEsNACAAIAJqQbABai0AAEUNACABQQJ0QbyowABqKAIAIQELIAAoAmgiBSAAKAKcASIGTw0DIAAoAmwhAiAALQC9AQ0BDAILIAJBAkHgrcAAEGwACyADQQhqIABBugFqLwAAOwEAIAMgACkAsgE3AwAgACgCGCEEIAAgAkG0pcAAEIoBIAUgBCAFRyADEDYLIANBCGogAEG6AWovAAA7AQAgAyAAKQCyATcDACAAIAJBlKXAABCKASAFIAEgAxARIgQNAQsgAC0AvwENASAAKAJsIQIgA0EIaiIEIABBugFqLwAAOwEAIAMgACkAsgE3AwAgACACQZSlwAAQigEgBUEBayABIAMQEUUEQCAEIABBsgFqIgRBCGovAAA7AQAgAyAEKQAANwMAIAAgAkGUpcAAEIoBIAVBAmsgASADEBEaCyAGQQFrIQEMAgsgACAEIAVqIgE2AmggASAGRw0CIAAtAL8BDQIgBkEBayEBDAELAkAgACgCbCICIAAoAqwBRwRAIAIgACgCoAFBAWtPDQEgACACQaSlwAAQigFBAToADCAAIAJBAWoiAjYCbAwBCyAAIAJBpKXAABCKAUEBOgAMIABBARCyASAAKAJsIQILIANBCGogAEG6AWovAAA7AQAgAyAAKQCyATcDACAAIAJBlKXAABCKAUEAIAEgAxARIQEgACgCbCECCyAAIAE2AmgLIAIgACgCZCIBSQRAIAAoAmAgAmpBAToAACADQRBqJAAPCyACIAFBiLDAABBsAAuPBAELfyABQQFrIQ0gACgCBCEKIAAoAgAhCyAAKAIIIQwDQAJAAkAgAiAESQ0AA0AgASAEaiEFAkACQCACIARrIgdBCE8EQAJAIAVBA2pBfHEiBiAFayIDBEBBACEAA0AgACAFai0AAEEKRg0FIAMgAEEBaiIARw0ACyAHQQhrIgAgA08NAQwDCyAHQQhrIQALA0AgBkEEaigCACIJQYqUqNAAc0GBgoQIayAJQX9zcSAGKAIAIglBipSo0ABzQYGChAhrIAlBf3NxckGAgYKEeHENAiAGQQhqIQYgACADQQhqIgNPDQALDAELIAIgBEYEQCACIQQMBAtBACEAA0AgACAFai0AAEEKRg0CIAcgAEEBaiIARw0ACyACIQQMAwsgAyAHRgRAIAIhBAwDCwNAIAMgBWotAABBCkYEQCADIQAMAgsgByADQQFqIgNHDQALIAIhBAwCCyAAIARqIgZBAWohBAJAIAIgBk0NACAAIAVqLQAAQQpHDQBBACEFIAQiBiEADAMLIAIgBE8NAAsLQQEhBSACIgAgCCIGRw0AQQAPCwJAIAwtAABFDQAgC0Gs+cAAQQQgCigCDBEBAEUNAEEBDwsgACAIayEHQQAhAyAAIAhHBEAgACANai0AAEEKRiEDCyABIAhqIQAgDCADOgAAIAYhCCALIAAgByAKKAIMEQEAIgAgBXJFDQALIAALugYBBX8jAEHAAWsiAiQAIAAoAgAhAyACQQxqIgBBrAFqQfyLwAA2AgAgAEGkAWpBhI7AADYCACAAQZwBakH0jcAANgIAIAJBoAFqQfSNwAA2AgAgAkGYAWpBhIvAADYCACACQZABakGEi8AANgIAIAJBiAFqQeSNwAA2AgAgAEH0AGpB5IzAADYCACACQfgAakHkjMAANgIAIAJB8ABqQeSMwAA2AgAgAEHcAGpB5IzAADYCACACQeAAakHUjcAANgIAIAJB2ABqQYSLwAA2AgAgAkHQAGpBxI3AADYCACACQcgAakHIjMAANgIAIAJBQGtBtI3AADYCACACQThqQaSNwAA2AgAgAEEkakGUjcAANgIAIAJBKGpBhI3AADYCACACQSBqQYSNwAA2AgAgAkEYakGEi8AANgIAIAIgA0HcAGo2AqwBIAIgA0GIAWo2AqQBIAIgA0H0AGo2ApwBIAIgA0GsAWo2ApQBIAIgA0GoAWo2AowBIAIgA0HBAWo2AoQBIAIgA0HAAWo2AnwgAiADQb8BajYCdCACIANBvgFqNgJsIAIgA0G9AWo2AmQgAiADQdAAajYCXCACIANBpAFqNgJUIAIgA0GwAWo2AkwgAiADQbIBajYCRCACIANB6ABqNgI8IAIgA0HIAGo2AjQgAiADQbwBajYCLCACIANBJGo2AiQgAiADNgIcIAIgA0GgAWo2AhQgAkGEi8AANgIQIAIgA0GcAWo2AgwgAiADQcIBajYCvAEgAiACQbwBajYCtAFBFiEGQdCPwAAhBCMAQSBrIgMkACADQRY2AgAgA0EWNgIEIAEoAhRBlI7AAEEIIAEoAhgoAgwRAQAhBSADQQA6AA0gAyAFOgAMIAMgATYCCAJ/A0AgA0EIaiAEKAIAIARBBGooAgAgAEHM+8AAECchBSAAQQhqIQAgBEEIaiEEIAZBAWsiBg0ACyADLQAMIQEgAUEARyADLQANRQ0AGkEBIAENABogBSgCACIALQAcQQRxRQRAIAAoAhRBu/nAAEECIAAoAhgoAgwRAQAMAQsgACgCFEG6+cAAQQEgACgCGCgCDBEBAAsgA0EgaiQAIAJBwAFqJAAL+AMBAn8gACABaiECAkACQCAAKAIEIgNBAXENACADQQJxRQ0BIAAoAgAiAyABaiEBIAAgA2siAEGAl8EAKAIARgRAIAIoAgRBA3FBA0cNAUH4lsEAIAE2AgAgAiACKAIEQX5xNgIEIAAgAUEBcjYCBCACIAE2AgAMAgsgACADECYLAkACQAJAIAIoAgQiA0ECcUUEQCACQYSXwQAoAgBGDQIgAkGAl8EAKAIARg0DIAIgA0F4cSICECYgACABIAJqIgFBAXI2AgQgACABaiABNgIAIABBgJfBACgCAEcNAUH4lsEAIAE2AgAPCyACIANBfnE2AgQgACABQQFyNgIEIAAgAWogATYCAAsgAUGAAk8EQCAAIAEQKw8LIAFBeHFB6JTBAGohAgJ/QfCWwQAoAgAiA0EBIAFBA3Z0IgFxRQRAQfCWwQAgASADcjYCACACDAELIAIoAggLIQEgAiAANgIIIAEgADYCDCAAIAI2AgwgACABNgIIDwtBhJfBACAANgIAQfyWwQBB/JbBACgCACABaiIBNgIAIAAgAUEBcjYCBCAAQYCXwQAoAgBHDQFB+JbBAEEANgIAQYCXwQBBADYCAA8LQYCXwQAgADYCAEH4lsEAQfiWwQAoAgAgAWoiATYCACAAIAFBAXI2AgQgACABaiABNgIACwuuBAEFfyMAQTBrIgUkACACIAFrIgggA0shCSACQQFrIgYgACgCHCIHQQFrSQRAIAAgBkG0psAAEIoBQQA6AAwLIAMgCCAJGyEDAkACQCABRQRAIABBDGohBiACIAdGDQEgACgCGCEBIAVBLGogBEEIai8AADsBACAFQqCAgIAQNwIcIAUgBCkAADcCJCAFQQhqIAVBHGogARBXIAVBADoAFCADBEAgACgCFCACaiAAKAIcayECA0AgBUEcaiIBIAVBCGoQYSAFQQA6ACggBigCCCIHIAYoAgBGBEAgBiAHQQEQhgELIAYoAgQgAkEEdGohBAJAIAIgB08EQCACIAdGDQEgAiAHEGsACyAEQRBqIAQgByACa0EEdBCIAgsgBCABKQIANwIAIAYgB0EBajYCCCAEQQhqIAFBCGopAgA3AgAgA0EBayIDDQALCyAFKAIIIgFFDQIgBSgCDCABQRRsQQQQ5QEMAgsgACABQQFrQcSmwAAQigFBADoADCAFIAAgASACQdSmwAAQZSAFKAIAIQYgBSgCBCIBIANJBEBBoJ7AAEEjQZCfwAAQnAEACyADIAYgA0EEdGogASADaxAVIAAgAiADayACIAQQUgwBCyAAKAIYIQIgBUEsaiAEQQhqLwAAOwEAIAVCoICAgBA3AhwgBSAEKQAANwIkIAVBCGoiASAFQRxqIAIQVyAFIAM2AhggBUEAOgAUIAYgARBACyAAQQE6ACAgBUEwaiQAC+cCAQV/AkBBzf97IABBECAAQRBLGyIAayABTQ0AQRAgAUELakF4cSABQQtJGyIEIABqQQxqEA8iAkUNACACQQhrIQECQCAAQQFrIgMgAnFFBEAgASEADAELIAJBBGsiBSgCACIGQXhxQQAgACACIANqQQAgAGtxQQhrIgAgAWtBEEsbIABqIgAgAWsiAmshAyAGQQNxBEAgACADIAAoAgRBAXFyQQJyNgIEIAAgA2oiAyADKAIEQQFyNgIEIAUgAiAFKAIAQQFxckECcjYCACABIAJqIgMgAygCBEEBcjYCBCABIAIQIQwBCyABKAIAIQEgACADNgIEIAAgASACajYCAAsCQCAAKAIEIgFBA3FFDQAgAUF4cSICIARBEGpNDQAgACAEIAFBAXFyQQJyNgIEIAAgBGoiASACIARrIgRBA3I2AgQgACACaiICIAIoAgRBAXI2AgQgASAEECELIABBCGohAwsgAwuLAwEHfyMAQRBrIgQkAAJAAkACQAJAAkACQCABKAIEIgJFDQAgASgCACEFIAJBA3EhBgJAIAJBBEkEQEEAIQIMAQsgBUEcaiEDIAJBfHEhCEEAIQIDQCADKAIAIANBCGsoAgAgA0EQaygCACADQRhrKAIAIAJqampqIQIgA0EgaiEDIAggB0EEaiIHRw0ACwsgBgRAIAdBA3QgBWpBBGohAwNAIAMoAgAgAmohAiADQQhqIQMgBkEBayIGDQALCyABKAIMBEAgAkEASA0BIAUoAgRFIAJBEElxDQEgAkEBdCECCyACDQELQQEhA0EAIQIMAQsgAkEASA0BQYmTwQAtAAAaIAJBARDXASIDRQ0CCyAEQQA2AgggBCADNgIEIAQgAjYCACAEQYTzwAAgARAdRQ0CQeTzwABBMyAEQQ9qQZj0wABBwPTAABBjAAsQqAEAC0EBIAJBxJPBACgCACIAQd4AIAAbEQIAAAsgACAEKQIANwIAIABBCGogBEEIaigCADYCACAEQRBqJAAL1QIBB39BASEJAkACQCACRQ0AIAEgAkEBdGohCiAAQYD+A3FBCHYhCyAAQf8BcSENA0AgAUECaiEMIAcgAS0AASICaiEIIAsgAS0AACIBRwRAIAEgC0sNAiAIIQcgCiAMIgFGDQIMAQsCQAJAIAcgCE0EQCAEIAhJDQEgAyAHaiEBA0AgAkUNAyACQQFrIQIgAS0AACABQQFqIQEgDUcNAAtBACEJDAULIAcgCEGYgMEAEO0BAAsgCCAEQZiAwQAQ6wEACyAIIQcgCiAMIgFHDQALCyAGRQ0AIAUgBmohAyAAQf//A3EhAQNAIAVBAWohAAJAIAUtAAAiAsAiBEEATgRAIAAhBQwBCyAAIANHBEAgBS0AASAEQf8AcUEIdHIhAiAFQQJqIQUMAQtBiIDBABDvAQALIAEgAmsiAUEASA0BIAlBAXMhCSADIAVHDQALCyAJQQFxC/MCAQR/IAAoAgwhAgJAAkAgAUGAAk8EQCAAKAIYIQMCQAJAIAAgAkYEQCAAQRRBECAAKAIUIgIbaigCACIBDQFBACECDAILIAAoAggiASACNgIMIAIgATYCCAwBCyAAQRRqIABBEGogAhshBANAIAQhBSABIgIoAhQhASACQRRqIAJBEGogARshBCACQRRBECABG2ooAgAiAQ0ACyAFQQA2AgALIANFDQIgACAAKAIcQQJ0QdiTwQBqIgEoAgBHBEAgA0EQQRQgAygCECAARhtqIAI2AgAgAkUNAwwCCyABIAI2AgAgAg0BQfSWwQBB9JbBACgCAEF+IAAoAhx3cTYCAAwCCyACIAAoAggiAEcEQCAAIAI2AgwgAiAANgIIDwtB8JbBAEHwlsEAKAIAQX4gAUEDdndxNgIADwsgAiADNgIYIAAoAhAiAQRAIAIgATYCECABIAI2AhgLIAAoAhQiAEUNACACIAA2AhQgACACNgIYCwuBAwIFfwF+IwBBQGoiBSQAQQEhBwJAIAAtAAQNACAALQAFIQggACgCACIGKAIcIglBBHFFBEAgBigCFEGz+cAAQbD5wAAgCBtBAkEDIAgbIAYoAhgoAgwRAQANASAGKAIUIAEgAiAGKAIYKAIMEQEADQEgBigCFEGA+cAAQQIgBigCGCgCDBEBAA0BIAMgBiAEKAIMEQAAIQcMAQsgCEUEQCAGKAIUQbX5wABBAyAGKAIYKAIMEQEADQEgBigCHCEJCyAFQQE6ABsgBSAGKQIUNwIMIAVBlPnAADYCNCAFIAVBG2o2AhQgBSAGKQIINwIkIAYpAgAhCiAFIAk2AjggBSAGKAIQNgIsIAUgBi0AIDoAPCAFIAo3AhwgBSAFQQxqIgY2AjAgBiABIAIQHw0AIAVBDGpBgPnAAEECEB8NACADIAVBHGogBCgCDBEAAA0AIAUoAjBBuPnAAEECIAUoAjQoAgwRAQAhBwsgAEEBOgAFIAAgBzoABCAFQUBrJAAgAAuGBAEFfyMAQRBrIgMkAAJAAn8CQCABQYABTwRAIANBADYCDCABQYAQSQ0BIAFBgIAESQRAIAMgAUE/cUGAAXI6AA4gAyABQQx2QeABcjoADCADIAFBBnZBP3FBgAFyOgANQQMMAwsgAyABQT9xQYABcjoADyADIAFBBnZBP3FBgAFyOgAOIAMgAUEMdkE/cUGAAXI6AA0gAyABQRJ2QQdxQfABcjoADEEEDAILIAAoAggiAiAAKAIARgRAIwBBIGsiBCQAAkACQCACQQFqIgJFDQAgACgCACIFQQF0IgYgAiACIAZJGyICQQggAkEISxsiAkF/c0EfdiEGIAQgBQR/IAQgBTYCHCAEIAAoAgQ2AhRBAQVBAAs2AhggBEEIaiAGIAIgBEEUahBPIAQoAggEQCAEKAIMIgBFDQEgACAEKAIQQcSTwQAoAgAiAEHeACAAGxECAAALIAQoAgwhBSAAIAI2AgAgACAFNgIEIARBIGokAAwBCxCoAQALIAAoAgghAgsgACACQQFqNgIIIAAoAgQgAmogAToAAAwCCyADIAFBP3FBgAFyOgANIAMgAUEGdkHAAXI6AAxBAgshASABIAAoAgAgACgCCCICa0sEQCAAIAIgARBEIAAoAgghAgsgACgCBCACaiADQQxqIAEQigIaIAAgASACajYCCAsgA0EQaiQAQQALugIBA38jAEGAAWsiBCQAAn8CQAJAIAEoAhwiAkEQcUUEQCACQSBxDQEgADUCACABECoMAwsgACgCACEAQQAhAgNAIAIgBGpB/wBqIABBD3EiA0EwciADQdcAaiADQQpJGzoAACACQQFrIQIgAEEQSSAAQQR2IQBFDQALDAELIAAoAgAhAEEAIQIDQCACIARqQf8AaiAAQQ9xIgNBMHIgA0E3aiADQQpJGzoAACACQQFrIQIgAEEQSSAAQQR2IQBFDQALIAJBgAFqIgBBgQFPBEAgAEGAAUHg+cAAEOoBAAsgAUHw+cAAQQIgAiAEakGAAWpBACACaxAYDAELIAJBgAFqIgBBgQFPBEAgAEGAAUHg+cAAEOoBAAsgAUHw+cAAQQIgAiAEakGAAWpBACACaxAYCyAEQYABaiQAC8ACAgV/AX4jAEEwayIEJABBJyECAkAgAEKQzgBUBEAgACEHDAELA0AgBEEJaiACaiIDQQRrIAAgAEKQzgCAIgdCkM4Afn2nIgVB//8DcUHkAG4iBkEBdEHy+cAAai8AADsAACADQQJrIAUgBkHkAGxrQf//A3FBAXRB8vnAAGovAAA7AAAgAkEEayECIABC/8HXL1YgByEADQALCyAHpyIDQeMASwRAIAenIgVB//8DcUHkAG4hAyACQQJrIgIgBEEJamogBSADQeQAbGtB//8DcUEBdEHy+cAAai8AADsAAAsCQCADQQpPBEAgAkECayICIARBCWpqIANBAXRB8vnAAGovAAA7AAAMAQsgAkEBayICIARBCWpqIANBMHI6AAALIAFBiPbAAEEAIARBCWogAmpBJyACaxAYIARBMGokAAvEAgEEfyAAQgA3AhAgAAJ/QQAgAUGAAkkNABpBHyABQf///wdLDQAaIAFBBiABQQh2ZyIDa3ZBAXEgA0EBdGtBPmoLIgI2AhwgAkECdEHYk8EAaiEEQQEgAnQiA0H0lsEAKAIAcUUEQCAEIAA2AgAgACAENgIYIAAgADYCDCAAIAA2AghB9JbBAEH0lsEAKAIAIANyNgIADwsCQAJAIAEgBCgCACIDKAIEQXhxRgRAIAMhAgwBCyABQQBBGSACQQF2ayACQR9GG3QhBQNAIAMgBUEddkEEcWpBEGoiBCgCACICRQ0CIAVBAXQhBSACIQMgAigCBEF4cSABRw0ACwsgAigCCCIBIAA2AgwgAiAANgIIIABBADYCGCAAIAI2AgwgACABNgIIDwsgBCAANgIAIAAgAzYCGCAAIAA2AgwgACAANgIIC7QCAQd/IwBBEGsiAiQAQQEhBwJAAkAgASgCFCIEQScgASgCGCgCECIFEQAADQAgAiAAKAIAQYECEBcCQCACLQAAQYABRgRAIAJBCGohBkGAASEDA0ACQCADQYABRwRAIAItAAoiACACLQALTw0EIAIgAEEBajoACiAAQQpPDQYgACACai0AACEBDAELQQAhAyAGQQA2AgAgAigCBCEBIAJCADcDAAsgBCABIAURAABFDQALDAILIAItAAoiAUEKIAFBCksbIQAgASACLQALIgMgASADSxshBgNAIAEgBkYNASACIAFBAWoiAzoACiAAIAFGDQMgASACaiEIIAMhASAEIAgtAAAgBREAAEUNAAsMAQsgBEEnIAURAAAhBwsgAkEQaiQAIAcPCyAAQQpBnIzBABBsAAvMAgACQAJAAkACQAJAAkACQCADQQFrDgYAAQIDBAUGCyAAKAIYIQMgACACQeSlwAAQigEiBEEAOgAMIAQgASADIAUQLiAAIAJBAWogACgCHCAFEFIPCyAAKAIYIQMgACACQfSlwAAQigFBACABQQFqIgEgAyABIANJGyAFEC4gAEEAIAIgBRBSDwsgAEEAIAAoAhwgBRBSDwsgACgCGCEDIAAgAkGEpsAAEIoBIgAgASADIAUQLiAAQQA6AAwPCyAAKAIYIQMgACACQZSmwAAQigFBACABQQFqIgAgAyAAIANJGyAFEC4PCyAAKAIYIQEgACACQaSmwAAQigEiAEEAIAEgBRAuIABBADoADA8LIAAoAhghAyAAIAJB1KXAABCKASIAIAEgASAEIAMgAWsiASABIARLG2oiASAFEC4gASADRgRAIABBADoADAsLuwIBAn8CQAJAAkACQAJAIAAoAggiBCABRg0AIAEgBE8NASAAKAIEIgUgAUEUbGooAgRFBEAgAUEBayIAIARPDQMgBSAAQRRsaiIAQqCAgIAQNwIAIAAgAykAADcACCAAQRBqIANBCGovAAA7AAALIAEgAksNAyACIARLDQQgBSACQRRsaiEAIAEgAkcEQCAFIAFBFGxqIQEgA0EIaiEFA0AgAUKggICAEDcCACABIAMpAAA3AAggAUEQaiAFLwAAOwAAIAAgAUEUaiIBRw0ACwsgAiAETw0AIAAoAgQNACAAQqCAgIAQNwIAIAAgAykAADcACCAAQRBqIANBCGovAAA7AAALDwsgASAEQYihwAAQbAALIAAgBEGYocAAEGwACyABIAJBqKHAABDtAQALIAIgBEGoocAAEOsBAAuUAgEDfyMAQRBrIgIkAAJAAn8CQCABQYABTwRAIAJBADYCDCABQYAQSQ0BIAFBgIAESQRAIAIgAUEMdkHgAXI6AAwgAiABQQZ2QT9xQYABcjoADUECIQNBAwwDCyACIAFBBnZBP3FBgAFy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aigCADYCACACQRBqJAALugEBAX8jAEEQayILJAAgACgCFCABIAIgACgCGCgCDBEBACEBIAtBADoADSALIAE6AAwgCyAANgIIIAtBCGogAyAEIAUgBhAnIAcgCCAJIAoQJyECIAstAAwhAQJ/IAFBAEcgCy0ADUUNABpBASABDQAaIAIoAgAiAC0AHEEEcUUEQCAAKAIUQbv5wABBAiAAKAIYKAIMEQEADAELIAAoAhRBuvnAAEEBIAAoAhgoAgwRAQALIAtBEGokAAuwAQEDf0EBIQRBBCEGAkAgAUUNACACQQBIDQACfwJAAkACfyADKAIEBEAgAygCCCIBRQRAIAJFBEAMBAtBiZPBAC0AABogAkEBENcBDAILIAMoAgAgAUEBIAIQzQEMAQsgAkUEQAwCC0GJk8EALQAAGiACQQEQ1wELIgRFDQELIAAgBDYCBEEADAELIABBATYCBEEBCyEEQQghBiACIQULIAAgBmogBTYCACAAIAQ2AgALwwEBAn8jAEFAaiICJAACQCABBEAgASgCACIDQX9GDQEgASADQQFqNgIAIAJBATYCFCACQeSGwAA2AhAgAkIBNwIcIAJBEzYCLCACIAFBBGo2AiggAiACQShqNgIYIAJBMGoiAyACQRBqECQgASABKAIAQQFrNgIAIAJBCGogAxDbASACKAIIIQEgAiACKAIMNgIEIAIgATYCACACKAIEIQEgACACKAIANgIAIAAgATYCBCACQUBrJAAPCxD+AQALEP8BAAu4AQEDfwJAIAAoAoQEIgFBf0cEQCABQQFqIQIgAUEgSQ0BIAJBIEHMl8AAEOsBAAtBzJfAABCqAQALIABBBGohASAAIAJBBHRqQQRqIQMDQAJAIAEoAgAiAkF/RwRAIAJBBkkNASACQQFqQQZB3JzAABDrAQALQdycwAAQqgEACyABQQRqQQAgAkEBdEECahCJAhogAUEANgIAIAMgAUEQaiIBRw0ACyAAQYCAxAA2AgAgAEEANgKEBAvmAgEEfyMAQSBrIgMkACADQQxqIQICQCABLQAgRQRAIAJBADYCAAwBCyABQQA6ACACQCABKAIABEAgASgCFCIFIAEoAhxrIgQgASgCCEsNAQsgAkEANgIADAELIAQgASgCBGsiBCAFTQRAIAFBADYCFCACIAQ2AgwgAiAFIARrNgIQIAIgAUEM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gYCAgHggBUUNABogBUECdCEJIAcoAgQhCgJAIARFBEBBBCEBIAogCUEEEOUBDAELQQQgCiAJQQQgBEECdCIFEM0BIgFFDQEaCyAHIAQ2AgAgByABNgIEQYGAgIB4CyEBIAggBTYCBCAIIAE2AgAgAkEgaiQADAELIAJBATYCDCACQYDswAA2AgggAkIANwIUIAJB3OvAADYCECACQQhqQdTswAAQowEACyADKAIIIgFBgYCAgHhGDQAgAUUNASABIAMoAgxBxJPBACgCACIAQd4AIAAbEQIAAAsgA0EQaiQADAELEKgBAAsgBkEIaiAHKQIENwMAIAYoAgghASAGIAYoAgw2AgQgBiABNgIAIAYoAgQhASAAIAYoAgA2AgAgACABNgIEIAZBIGokAAtxAQF/IwBBEGsiAiQAIAIgAEEgajYCDCABQayLwABBBkGyi8AAQQUgAEEMakG4i8AAQciLwABBBCAAQRhqQcyLwABBBCAAQRxqQYSLwABB0IvAAEEQIABB4IvAAEHwi8AAQQsgAkEMahA6IAJBEGokAAtxAQF/IwBBEGsiAiQAIAIgAEETajYCDCABQaeMwABBCEGvjMAAQQogAEGEi8AAQbmMwABBCiAAQQRqQcOMwABBAyAAQQhqQciMwABB2IzAAEELIABBEmpB5IzAAEH0jMAAQQ4gAkEMahA6IAJBEGokAAtvAQF/IwBBMGsiAiQAIAIgATYCBCACIAA2AgAgAkEsakHdADYCACACQQM2AgwgAkGU9cAANgIIIAJCAjcCFCACQd0ANgIkIAIgAkEgajYCECACIAJBBGo2AiggAiACNgIgIAJBCGpB4JbAABCjAQALbAEBfyMAQTBrIgMkACADIAE2AgQgAyAANgIAIANBLGpB3QA2AgAgA0ECNgIMIANB0PfAADYCCCADQgI3AhQgA0HdADYCJCADIANBIGo2AhAgAyADNgIoIAMgA0EEajYCICADQQhqIAIQowEAC2YBAn8jAEEQayICJAAgACgCACIDQQFqIQACfyADLQAARQRAIAIgADYCCCABQaGCwABBByACQQhqQaiCwAAQQwwBCyACIAA2AgwgAUG4gsAAQQMgAkEMakG8gsAAEEMLIAJBEGokAAtiAQN/IwBBEGsiAyQAIAEoAgghBCADQQhqIAEoAgAgAjUCABBYIAMoAgwhAiAD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QSBrIgMkACMAQSBrIgQkACAAQQRqIgUgASACED0gBEEUaiICIAUQdiAEQQhqIAUQTSAEKQMIIQYgA0EIaiIBQQhqIAJBCGooAgA2AgAgASAEKQIUNwIAIAEgBjcCDCAEQSBqJAAgA0EANgIcIAMgA0EcaiABEDQgAygCBCEBIAMoAgAEQCADIAE2AhxBlIPAAEErIANBHGpBwIPAAEHUhsAAEGMACyADQQhqEKYBIANBIGokACAAQQA2AgAgAQ8LEP4BAAsQ/wEACzsBAX8CQCACQX9HBEAgAkEBaiEEIAJBIEkNASAEQSAgAxDrAQALIAMQqgEACyAAIAQ2AgQgACABNgIACzkAAkACfyACQYCAxABHBEBBASAAIAIgASgCEBEAAA0BGgsgAw0BQQALDwsgACADIAQgASgCDBEBAAvUAgEDfyAAKAIAIQAgASgCHCIDQRBxRQRAIANBIHFFBEAgADMBACABECoPCyMAQYABayIDJAAgAC8BACECQQAhAANAIAAgA2pB/wBqIAJBD3EiBEEwciAEQTdqIARBCkkbOgAAIABBAWshACACQf//A3EiBEEEdiECIARBEE8NAAsgAEGAAWoiAkGBAU8EQCACQYABQeD5wAAQ6gEACyABQfD5wABBAiAAIANqQYABakEAIABrEBggA0GAAWokAA8LIwBBgAFrIgMkACAALwEAIQJBACEAA0AgACADakH/AGogAkEPcSIEQTByIARB1wBqIARBCkkbOgAAIABBAWshACACQf//A3EiBEEEdiECIARBEE8NAAsgAEGAAWoiAkGBAU8EQCACQYABQeD5wAAQ6gEACyABQfD5wABBAiAAIANqQYABakEAIABrEBggA0GAAWokAAs3AQF/IAAoAgAhACABKAIcIgJBEHFFBEAgAkEgcUUEQCAAIAEQ7gEPCyAAIAEQVA8LIAAgARBVCzcBAX8gACgCACEAIAEoAhwiAkEQcUUEQCACQSBxRQRAIAAgARDsAQ8LIAAgARBWDwsgACABEFMLsAIBAn8jAEEgayICJAAgAkEBOwEcIAIgATYCGCACIAA2AhQgAkGM98AANgIQIAJBiPbAADYCDCMAQRBrIgEkACACQQxqIgAoAggiAkUEQEG08cAAEO8BAAsgASAAKAIMNgIMIAEgADYCCCABIAI2AgQjAEEQayIAJAAgAUEE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QZixwAAQnAEACyACIAAgAkEUbGogASACaxAcCyUAIABBATYCBCAAIAEoAgQgASgCAGtBBHYiATYCCCAAIAE2AgALJQAgAEUEQEHQlcAAQTIQ/QEACyAAIAIgAyAEIAUgASgCEBEIAAswACABKAIUIAAtAABBAnQiAEH8h8AAaigCACAAQcSHwABqKAIAIAEoAhgoAgwRAQALMAAgASgCFCAALQAAQQJ0IgBBuJLAAGooAgAgAEGsksAAaigCACABKAIYKAIMEQEACyMAIABFBEBB0JXAAEEyEP0BAAsgACACIAMgBCABKAIQEQUACyMAIABFBEBB0JXAAEEyEP0BAAsgACACIAMgBCABKAIQERgACyMAIABFBEBB0JXAAEEyEP0BAAsgACACIAMgBCABKAIQERoACyMAIABFBEBB0JXAAEEyEP0BAAsgACACIAMgBCABKAIQERwACyMAIABFBEBB0JXAAEEyEP0BAAsgACACIAMgBCABKAIQEQoACygBAX8gACgCACIBQYCAgIB4ckGAgICAeEcEQCAAKAIEIAFBARDlAQsLLgAgASgCFEGgjMAAQZuMwAAgACgCAC0AACIAG0EHQQUgABsgASgCGCgCDBEBAAshACAARQRAQdCVwABBMhD9AQALIAAgAiADIAEoAhARAwALHQEBfyAAKAIAIgEEQCAAKAIEIAFBAnRBBBDlAQsLHQEBfyAAKAIAIgEEQCAAKAIEIAFBFGxBBBDlAQsLHQEBfyAAKAIAIgEEQCAAKAIEIAFBBHRBBBDlAQsLIgAgAC0AAEUEQCABQdz7wABBBRAWDwsgAUHh+8AAQQQQFgsrACABKAIUQYeRwABBgJHAACAALQAAIgAbQQlBByAAGyABKAIYKAIMEQEACysAIAEoAhRBuJHAAEGMjMAAIAAtAAAiABtBC0EGIAAbIAEoAhgoAgwRAQALHwAgAEUEQEHQlcAAQTIQ/QEACyAAIAIgASgCEBEAAAsbABAHIQIgAEEANgIIIAAgAjYCBCAAIAE2AgALwQMCAn4Gf0GMk8EAKAIARQRAIwBBMGsiAyQAAn8CQCAABEAgACgCACAAQQA2AgANAQsgA0EQakGQlMAAKQMANwMAIANBiJTAACkDADcDCEEADAELIANBEGogAEEQaikCADcDACADIAApAgg3AwggACgCBAshAEGMk8EA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IgIgAy0AG08NBCADIAJBAWo6ABogAkEKTw0GIANBEGogAmotAAAhAQwBC0EAIQcgDEEANgIAIAMoAhQhASADQgA3AxALIAogASALEQAARQ0ACwwHCyADLQAaIgFBCiABQQpLGyECIAEgAy0AGyIHIAEgB0sbIQwDQCABIAxGDQEgAyABQQFqIgc6ABogASACRg0DIANBEGogAWohECAHIQEgCiAQLQAAIAsRAABFDQALDAYLAn9BASAGQYABSQ0AGkECIAZBgBBJDQAaQQNBBCAGQYCABEkbCyAFaiECCyAFIAlrIABqIQUgACAPRw0BDAILCyACQQpBnIzBABBsAAsgAkUEQEEAIQIMAQsgAiAETwRAIAIgBEYNAQwDCyACIAhqLAAAQb9/TA0CCyAKIAIgCGogBCACayAOKAIMEQEADQAgCkEiIAsRAAAhDQsgA0EgaiQADAILIAggBCACIARB6PvAABDVAQALIAggBCACIAVB+PvAABDVAQALIA0LFAAgAEEANgIIIABCgICAgBA3AgALEQAgACgCBCAAKAIIIAEQhgILqgIBB38jAEEQayIFJAACQAJAAkAgASgCCCIDIAEoAgBPDQAgBUEIaiEGIwBBIGsiAiQAAkAgASgCACIEIANPBEACf0GBgICAeCAERQ0AGiABKAIEIQcCQCADRQRAQQEhCCAHIARBARDlAQwBC0EBIAcgBEEBIAMQzQEiCEUNARoLIAEgAzYCACABIAg2AgRBgYCAgHgLIQQgBiADNgIEIAYgBDYCACACQSBqJAAMAQsgAkEBNgIMIAJB9O3AADYCCCACQgA3AhQgAkHQ7cAANgIQIAJBCGpByO7AABCjAQALIAUoAggiAkGBgICAeEYNACACRQ0BIAIgBSgCDEHEk8EAKAIAIgBB3gAgABsRAgAACyAFQRBqJAAMAQsQqAEACyAAIAEpAgQ3AwALDgAgACABIAEgAmoQjwELIAAgAEKN04Cn1Nuixjw3AwggAELVnsTj3IPBiXs3AwALIgAgAELiq87AwdHBlKl/NwMIIABCivSnla2v+57uADcDAAsgACAAQsH3+ejMk7LRQTcDCCAAQuTex4WQ0IXefTcDAAsTACAAQdTxwAA2AgQgACABNgIACxAAIAEgACgCACAAKAIEEBYLEAAgASgCFCABKAIYIAAQHQupAQEDfyAA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AgICAgICAgICAgICAgICAgICAgICAgJzAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICdHUCAgICAgICdnc5OTk5OTk5OTk5OTk5OTk5OTk5OTk5OTk5OTk5OTk5OTk5OTk5OTk5OTk5OTk5OXg5OTk5OTk5OTl5egICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICezk5fDk5fQICAgICAgICAgICAgICAgICAgJ+AgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICfwICAoCBggICAgICAgICAgICAgICAoOEAgICAgICAgICAoWGdQIChwICAogCAgICAgICiYoCAgICAgICAgICAgICi4wCjY4Cj5CRkpOUlZYClwICmJmamwICAgICAgICAgI5OTk5OTk5OTk5OTk5OTk5OTk5OTk5OTk5OTk5OTk5OTk5OTk5OTk5OTk5OTk5OTk5OTk5OTk5OTk5OTk5OTmcHR0dHR0dHR0dHR0dHR0dHR0dHR0dHR0dHR0dHR0dHR0CAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgCdAgICAp6fAgQCBQYHCAkKCwwNDg8QERITFBUWFxgZGhscHQICHgICAgICAgIfICEiIwIkJSYnKCkCKgICAgKgoaKjpKWmLqeoqaqrrK0zAgICAgICrgICNTY3Ajg5Ojs8PT6vOTk5OTk5OTk5OTk5OTk5OTk5OTk5OTk5OTk5OTk5OTk5OTk5OTk5OTk5OTk5OTk5OTk5OTk5OTk5OTk5OTk5OTk5OTk5OTk5OTk5OTk5OTk5TAICAgICsE5PsYWGdQIChwICAogCAgICAgICiYoCAgICAgICAgICAgICi4yys44Cj5CRkpOUlZYClwICmJmamwICAgICAgICAgJVVXVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVUVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVUAQby/wAALKVVVVVUVAFBVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVUBAEHvv8AAC8QBEEEQVVVVVVVXVVVVVVVVVVVVUVVVAABAVPXdVVVVVVVVVVUVAAAAAABVVVVV/F1VVVVVVVVVVVVVVVVVVVVVVVVVVVVVVQUAFAAUBFBVVVVVVVVVFVFVVVVVVVVVAAAAAAAAQFVVVVVVVVVVVdVXVVVVVVVVVVVVVVUFAABUVVVVVVVVVVVVVVVVVRUAAFVVUVVVVVVVBRAAAAEBUFVVVVVVVVVVVVUBVVVVVVX/////f1VVVVBVAABVVVVVVVVVVVVVBQBBwMHAAAuYBEBVVVVVVVVVVVVVVVVVRVQBAFRRAQBVVQVVVVVVVVVVUVVVVVVVVVVVVVVVVVVVRAFUVVFVFVVVBVVVVVVVVUVBVVVVVVVVVVVVVVVVVVVUQRUUUFFVVVVVVVVVUFFVVUFVVVVVVVVVVVVVVVVVVVQBEFRRVVVVVQVVVVVVVQUAUVVVVVVVVVVVVVVVVVVVBAFUVVFVAVVVBVVVVVVVVVVFVVVVVVVVVVVVVVVVVVVFVFVVUVUVVVVVVVVVVVVVVVRUVVVVVVVVVVVVVVVVVQRUBQRQVUFVVQVVVVVVVVVVUVVVVVVVVVVVVVVVVVVVFEQFBFBVQVVVBVVVVVVVVVVQVVVVVVVVVVVVVVVVVRVEAVRVQVUVVVUFVVVVVVVVVVFVVVVVVVVVVVVVVVVVVVVVVUUVBURVFVVVVVVVVVVVVVVVVVVVVVVVVVVVUQBAVVUVAEBVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVRAABUVVUAQFVVVVVVVVVVVVVVVVVVVVVVVVBVVVVVVVURUVVVVVVVVVVVVVVVVVUBAABAAARVAQAAAQAAAAAAAAAAVFVFVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVQEEAEFBVVVVVVVVUAVUVVVVAVRVVUVBVVFVVVVRVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqAEGAxsAAC5ADVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVUBVVVVVVVVVVVVVVVVBVRVVVVVVVUFVVVVVVVVVQVVVVVVVVVVBVVVVX///ff//ddfd9bV11UQAFBVRQEAAFVXUVVVVVVVVVVVVVUVAFVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVQVVVVVVVVVVVUVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVQBVUVUVVAVVVVVVVVVVVVVVVVVVVVVVVVVVVVVcVFFVVVVVVVVVVVVVVVVVVRQBARAEAVBUAABRVVVVVVVVVVVVVVVUAAAAAAAAAQFVVVVVVVVVVVVVVVQBVVVVVVVVVVVVVVVUAAFAFVVVVVVVVVVVVFQAAVVVVUFVVVVVVVVUFUBBQVVVVVVVVVVVVVVVVVUVQEVBVVVVVVVVVVVVVVVVVVQAABVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVQAAAAAQAVFFVVFBVVVVVVVVVVVVVVVVVVVVVVQBBoMnAAAuTCFVVFQBVVVVVVVUFQFVVVVVVVVVVVVVVVQAAAABVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVUAAAAAAAAAAFRVVVVVVVVVVVX1VVVVaVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVV/VfXVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVX1VVVVVVV9VVVVVVVVVVVVVVVX///9VVVVVVVVVVVVV1VVVVVXVVVVVXVX1VVVVVX1VX1V1VVdVVVVVdVX1XXVdVV31VVVVVVVVVVdVVVVVVVVVVXfV31VVVVVVVVVVVVVVVVVVVf1VVVVVVVVXVVXVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVdVXVVVVVVVVVVVVVVVVV11VVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVFVBVVVVVVVVVVVVVVVVVVVX9////////////////X1XVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVQAAAAAAAAAAqqqqqqqqmqqqqqqqqqqqqqqqqqqqqqqqqqqqqqpVVVWqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqlpVVVVVVVWqqqqqqqqqqqqqqqqqqgoAqqqqaqmqqqqqqqqqqqqqqqqqqqqqqqqqqmqBqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqlWpqqqqqqqqqqqqqqmqqqqqqqqqqqqqqqqoqqqqqqqqqqqqaqqqqqqqqqqqqqqqqqqqqqqqqqqqqlVVlaqqqqqqqqqqqqqqaqqqqqqqqqqqqqpVVaqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqpVVVVVVVVVVVVVVVVVVVVVqqqqVqqqqqqqqqqqqqqqqqpqVVVVVVVVVVVVVVVVVV9VVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVUVQAAAUFVVVVVVVVUFVVVVVVVVVVVVVVVVVVVVVVVVVVVQVVVVRUUVVVVVVVVVQVVUVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVBVVVVVVVUAAAAAUFVFFVVVVVVVVVVVVQUAUFVVVVVVFQAAUFVVVaqqqqqqqqpWQFVVVVVVVVVVVVVVFQVQUFVVVVVVVVVVVVFVVVVVVVVVVVVVVVVVVVVVAUBBQVVVFVVVVFVVVVVVVVVVVVVVVFVVVVVVVVVVVVVVVQQUVAVRVVVVVVVVVVVVVVBVRVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVFUUVVVVVWqqqqqqqqqqqpVVVUAAAAAAEAVAEG/0cAAC+EMVVVVVVVVVVVFVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVAAAA8KqqWlUAAAAAqqqqqqqqqqpqqqqqqmqqVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVFamqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqlZVVVVVVVVVVVVVVVVVVQVUVVVVVVVVVVVVVVVVVVVVqmpVVQAAVFVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVUVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVRVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVUFQFUBQVUAVVVVVVVVVVVVVUAVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVBVVVVVVVV1VVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVUAVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVUVVFVVVVVVVVVVVVVVVVVVVVVVVVUBVVVVVVVVVVVVVVVVVVVVVVUFAABUVVVVVVVVVVVVVVUFUFVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVFVVVVVVVVVVVVVVVVVAAAAQFVVVVVVVVVVVVUUVFUVUFVVVVVVVVVVVVVVFUBBVUVVVVVVVVVVVVVVVVVVVVVAVVVVVVVVVVUVAAEAVFVVVVVVVVVVVVVVVVVVFVVVVVBVVVVVVVVVVVVVVVUFAEAFVQEUVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVUVUARVRVFVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVRUVAEBVVVVVVVBVVVVVVVVVVVVVVVVVFURUVVVVVRVVVVUFAFQAVFVVVVVVVVVVVVVVVVVVVVUAAAVEVVVVVVVFVVVVVVVVVVVVVVVVVVVVVVVVVVUUAEQRBFVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVFQVQVRBUVVVVVVVVUFVVVVVVVVVVVVVVVVVVVVVVVVVVFQBAEVRVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVFVEAEFVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVUBBRAAVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVUVAABBVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVUVRUEEVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVQAFVVRVVVVVVVVVAQBAVVVVVVVVVVVVFQAEQFUVVVUBQAFVVVVVVVVVVVVVAAAAAEBQVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVQBAABBVVVVVVVVVVVVVVVVVVVVVVVVVVQUAAAAAAAUABEFVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVUBQEUQAABVVVVVVVVVVVVVVVVVVVVVVVVQEVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVRVUVVVAVVVVVVVVVVVVVVVVBUBVRFVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVUFQAAAFBVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVQBUVVVVVVVVVVVVVVVVVVUAQFVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVUVVVVVVVVVVVVVVVVVVVVVFUBVVVVVVVVVVVVVVVVVVVVVVVVVqlRVVVpVVVWqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqpVVaqqqqqqqqqqqqqqqqqqqqqqqqqqqlpVVVVVVVVVVVVVqqpWVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVqqmqaaqqqqqqqqqqalVVVWVVVVVVVVVVallVVVWqVVWqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqlVVVVVVVVVVQQBVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVQBBq97AAAt1UAAAAAAAQFVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVRFQBQAAAABAAQBVVVVVVVVVBVBVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVUFVFVVVVVVVVVVVVVVVVVVAEGt38AACwJAFQBBu9/AAAvFBlRVUVVVVVRVVVVVFQABAAAAVVVVVVVVVVVVVVVVVVVVVVVVVVUAQAAAAAAUABAEQFVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVRVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVFVVVVVVVVVVVVVVVVVVVVAFVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVUAVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVAEBVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVUAQFVVVVVVVVVVVVVVVVVVV1VVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVXVVVVVVVVVVVVVVVVVVVVVdf3/f1VVVVVVVVVVVVVVVVVVVVVVVfX///////9uVVVVqqq6qqqqqur6v79VqqpWVV9VVVWqWlVVVVVVVf//////////V1VV/f/f///////////////////////3//////9VVVX/////////////f9X/VVVV/////1dX//////////////////////9/9//////////////////////////////////////////////////////////////X////////////////////X1VV1X////////9VVVVVdVVVVVVVVX1VVVVXVVVVVVVVVVVVVVVVVVVVVVVVVVXV////////////////////////////VVVVVVVVVVVVVVVV//////////////////////9fVVd//VX/VVXVV1X//1dVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVX///9VV1VVVVVVVf//////////////f///3/////////////////////////////////////////////////////////////9VVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVV////V///V1X//////////////9//X1X1////Vf//V1X//1dVqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqlpVVVVVVVVVVVmWVWGqpVmqVVVVVVWVVVVVVVVVVZVVVQBBjubAAAsBAwBBnObAAAvqLFVVVVVVlVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVFQCWalpaaqoFQKZZlWVVVVVVVVVVVQAAAABVVlVVqVZVVVVVVVVVVVVWVVVVVVVVVVUAAAAAAAAAAFRVVVWVWVlVVWVVVWlVVVVVVVVVVVVVVZVWlWqqqqpVqqpaVVVVWVWqqqpVVVVVZVVVWlVVVVWlZVZVVVWVVVVVVVVVppaalllZZamWqqpmVapVWllVWlZlVVVVaqqlpVpVVVWlqlpVVVlZVVVZVVVVVVWVVVVVVVVVVVVVVVVVVVVVVVVVVVVlVfVVVVVpVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqpqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqpVqqqqqqqqqqqqVVVVqqqqqqVaVVWaqlpVpaVVWlqllqVaVVVVpVpVlVVVVX1VaVmlVV9VZlVVVVVVVVVVZlX///9VVVWammqaVVVV1VVVVVXVVVWlXVX1VVVVVb1Vr6q6qquqqppVuqr6rrquVV31VVVVVVVVVVdVVVVVWVVVVXfV31VVVVVVVVWlqqpVVVVVVVXVV1VVVVVVVVVVVVVVVVetWlVVVVVVVVVVVaqqqqqqqqpqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqAAAAwKqqWlUAAAAAqqqqqqqqqqpqqqqqqmqqVVVVVVVVVVVVVVVVBVRVVVVVVVVVVVVVVVVVVVWqalVVAABUWaqqalWqqqqqqqqqWqqqqqqqqqqqqqqqqqqqWlWqqqqqqqqquv7/v6qqqqpWVVVVVVVVVVVVVVVVVfX///////9Kc1ZhbHVlKCkAAADANRAACAAAAMg1EAABAAAAVHJpZWQgdG8gc2hyaW5rIHRvIGEgbGFyZ2VyIGNhcGFjaXR53DUQACQAAAAvcnVzdGMvOWIwMDk1NmU1NjAwOWJhYjJhYTE1ZDdiZmYxMDkxNjU5OWUzZDZkNi9saWJyYXJ5L2FsbG9jL3NyYy9yYXdfdmVjLnJzCDYQAEwAAADnAQAACQAAAG51bGwgcG9pbnRlciBwYXNzZWQgdG8gcnVzdHJlY3Vyc2l2ZSB1c2Ugb2YgYW4gb2JqZWN0IGRldGVjdGVkIHdoaWNoIHdvdWxkIGxlYWQgdG8gdW5zYWZlIGFsaWFzaW5nIGluIHJ1c3QAAFRyaWVkIHRvIHNocmluayB0byBhIGxhcmdlciBjYXBhY2l0edA2EAAkAAAAL3J1c3RjLzliMDA5NTZlNTYwMDliYWIyYWExNWQ3YmZmMTA5MTY1OTllM2Q2ZDYvbGlicmFyeS9hbGxvYy9zcmMvcmF3X3ZlYy5yc/w2EABMAAAA5wEAAAkAAABaAAAADAAAAAQAAABbAAAAXAAAAAQAAABfAAAADAAAAAQAAABgAAAAYQAAAGIAAAAvcnVzdC9kZXBzL2RsbWFsbG9jLTAuMi42L3NyYy9kbG1hbGxvYy5yc2Fzc2VydGlvbiBmYWlsZWQ6IHBzaXplID49IHNpemUgKyBtaW5fb3ZlcmhlYWQAiDcQACkAAACoBAAACQAAAGFzc2VydGlvbiBmYWlsZWQ6IHBzaXplIDw9IHNpemUgKyBtYXhfb3ZlcmhlYWQAAIg3EAApAAAArgQAAA0AAABBY2Nlc3NFcnJvcm1lbW9yeSBhbGxvY2F0aW9uIG9mICBieXRlcyBmYWlsZWQAAAA7OBAAFQAAAFA4EAANAAAAbGlicmFyeS9zdGQvc3JjL2FsbG9jLnJz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AODhVa7iYVfs5CFZ0OihWSAA7lnwAX9aAHAABwAtAQEBAgECAQFICzAVEAFlBwIGAgIBBCMBHhtbCzoJCQEYBAEJAQMBBSsDPAgqGAEgNwEBAQQIBAEDBwoCHQE6AQEBAgQIAQkBCgIaAQICOQEEAgQCAgMDAR4CAwELAjkBBAUBAgQBFAIWBgEBOgEBAgEECAEHAwoCHgE7AQEBDAEJASgBAwE3AQEDBQMBBAcCCwIdAToBAgECAQMBBQIHAgsCHAI5AgEBAgQIAQkBCgIdAUgBBAECAwEBCAFRAQIHDAhiAQIJCwdJAhsBAQEBATcOAQUBAgULASQJAWYEAQYBAgICGQIEAxAEDQECAgYBDwEAAwADHQIeAh4CQAIBBwgBAgsJAS0DAQF1AiIBdgMEAgkBBgPbAgIBOgEBBwEBAQECCAYKAgEwHzEEMAcBAQUBKAkMAiAEAgIBAzgBAQIDAQEDOggCApgDAQ0BBwQBBgEDAsZAAAHDIQADjQFgIAAGaQIABAEKIAJQAgABAwEEARkCBQGXAhoSDQEmCBkLLgMwAQIEAgInAUMGAgICAgwBCAEvATMBAQMCAgUCAQEqAggB7gECAQQBAAEAEBAQAAIAAeIBlQUAAwECBQQoAwQBpQIABAACUANGCzEEewE2DykBAgIKAzEEAgIHAT0DJAUBCD4BDAI0CQoEAgFfAwIBAQIGAQIBnQEDCBUCOQIBAQEBFgEOBwMFwwgCAwEBFwFRAQIGAQECAQECAQLrAQIEBgIBAhsCVQgCAQECagEBAQIGAQFlAwIEAQUACQEC9QEKAgEBBAGQBAICBAEgCigGAgQIAQkGAgMuDQECAAcBBgEBUhYCBwECAQJ6BgMBAQIBBwEBSAIDAQEBAAILAjQFBQEBAQABBg8ABTsHAAE/BFEBAAIALgIXAAEBAwQFCAgCBx4ElAMANwQyCAEOARYFAQ8ABwERAgcBAgEFZAGgBwABPQQABAAHbQcAYIDwAHsJcHJvZHVjZXJzAghsYW5ndWFnZQEEUnVzdAAMcHJvY2Vzc2VkLWJ5AwVydXN0Yx0xLjc4LjAgKDliMDA5NTZlNSAyMDI0LTA0LTI5KQZ3YWxydXMGMC4yMC4zDHdhc20tYmluZGdlbhIwLjIuOTIgKDJhNGE0OTM2MikALA90YXJnZXRfZmVhdHVyZXMCKw9tdXRhYmxlLWdsb2JhbHMrCHNpZ24tZXh0");

          var loadVt = async () => {
                  await __wbg_init(wasm_code);
                  return exports$1;
              };

  class Clock {
    constructor() {
      let speed = arguments.length > 0 && arguments[0] !== undefined ? arguments[0] : 1.0;
      this.speed = speed;
      this.startTime = performance.now();
    }
    getTime() {
      return this.speed * (performance.now() - this.startTime) / 1000.0;
    }
    setTime(time) {
      this.startTime = performance.now() - time / this.speed * 1000.0;
    }
  }
  class NullClock {
    constructor() {}
    getTime(_speed) {}
    setTime(_time) {}
  }

  // Efficient array transformations without intermediate array objects.
  // Inspired by Elixir's streams and Rust's iterator adapters.

  class Stream {
    constructor(input, xfs) {
      this.input = typeof input.next === "function" ? input : input[Symbol.iterator]();
      this.xfs = xfs ?? [];
    }
    map(f) {
      return this.transform(Map$1(f));
    }
    flatMap(f) {
      return this.transform(FlatMap(f));
    }
    filter(f) {
      return this.transform(Filter(f));
    }
    take(n) {
      return this.transform(Take(n));
    }
    drop(n) {
      return this.transform(Drop(n));
    }
    transform(f) {
      return new Stream(this.input, this.xfs.concat([f]));
    }
    multiplex(other, comparator) {
      return new Stream(new Multiplexer(this[Symbol.iterator](), other[Symbol.iterator](), comparator));
    }
    toArray() {
      return Array.from(this);
    }
    [Symbol.iterator]() {
      let v = 0;
      let values = [];
      let flushed = false;
      const xf = compose(this.xfs, val => values.push(val));
      return {
        next: () => {
          if (v === values.length) {
            values = [];
            v = 0;
          }
          while (values.length === 0) {
            const next = this.input.next();
            if (next.done) {
              break;
            } else {
              xf.step(next.value);
            }
          }
          if (values.length === 0 && !flushed) {
            xf.flush();
            flushed = true;
          }
          if (values.length > 0) {
            return {
              done: false,
              value: values[v++]
            };
          } else {
            return {
              done: true
            };
          }
        }
      };
    }
  }
  function Map$1(f) {
    return emit => {
      return input => {
        emit(f(input));
      };
    };
  }
  function FlatMap(f) {
    return emit => {
      return input => {
        f(input).forEach(emit);
      };
    };
  }
  function Filter(f) {
    return emit => {
      return input => {
        if (f(input)) {
          emit(input);
        }
      };
    };
  }
  function Take(n) {
    let c = 0;
    return emit => {
      return input => {
        if (c < n) {
          emit(input);
        }
        c += 1;
      };
    };
  }
  function Drop(n) {
    let c = 0;
    return emit => {
      return input => {
        c += 1;
        if (c > n) {
          emit(input);
        }
      };
    };
  }
  function compose(xfs, push) {
    return xfs.reverse().reduce((next, curr) => {
      const xf = toXf(curr(next.step));
      return {
        step: xf.step,
        flush: () => {
          xf.flush();
          next.flush();
        }
      };
    }, toXf(push));
  }
  function toXf(xf) {
    if (typeof xf === "function") {
      return {
        step: xf,
        flush: () => {}
      };
    } else {
      return xf;
    }
  }
  class Multiplexer {
    constructor(left, right, comparator) {
      this.left = left;
      this.right = right;
      this.comparator = comparator;
    }
    [Symbol.iterator]() {
      let leftItem;
      let rightItem;
      return {
        next: () => {
          if (leftItem === undefined && this.left !== undefined) {
            const result = this.left.next();
            if (result.done) {
              this.left = undefined;
            } else {
              leftItem = result.value;
            }
          }
          if (rightItem === undefined && this.right !== undefined) {
            const result = this.right.next();
            if (result.done) {
              this.right = undefined;
            } else {
              rightItem = result.value;
            }
          }
          if (leftItem === undefined && rightItem === undefined) {
            return {
              done: true
            };
          } else if (leftItem === undefined) {
            const value = rightItem;
            rightItem = undefined;
            return {
              done: false,
              value: value
            };
          } else if (rightItem === undefined) {
            const value = leftItem;
            leftItem = undefined;
            return {
              done: false,
              value: value
            };
          } else if (this.comparator(leftItem, rightItem)) {
            const value = leftItem;
            leftItem = undefined;
            return {
              done: false,
              value: value
            };
          } else {
            const value = rightItem;
            rightItem = undefined;
            return {
              done: false,
              value: value
            };
          }
        }
      };
    }
  }

  async function parse$2(data) {
    if (data instanceof Response) {
      const text = await data.text();
      const result = parseJsonl(text);
      if (result !== undefined) {
        const {
          header,
          events
        } = result;
        if (header.version === 2) {
          return parseAsciicastV2(header, events);
        } else if (header.version === 3) {
          return parseAsciicastV3(header, events);
        } else {
          throw `asciicast v${header.version} format not supported`;
        }
      } else {
        const header = JSON.parse(text);
        if (header.version === 1) {
          return parseAsciicastV1(header);
        }
      }
    } else if (typeof data === "object" && data.version === 1) {
      return parseAsciicastV1(data);
    } else if (Array.isArray(data)) {
      const header = data[0];
      if (header.version === 2) {
        const events = data.slice(1, data.length);
        return parseAsciicastV2(header, events);
      } else if (header.version === 3) {
        const events = data.slice(1, data.length);
        return parseAsciicastV3(header, events);
      } else {
        throw `asciicast v${header.version} format not supported`;
      }
    }
    throw "invalid data";
  }
  function parseJsonl(jsonl) {
    const lines = jsonl.split("\n");
    let header;
    try {
      header = JSON.parse(lines[0]);
    } catch (_error) {
      return;
    }
    const events = new Stream(lines).drop(1).filter(l => l[0] === "[").map(JSON.parse);
    return {
      header,
      events
    };
  }
  function parseAsciicastV1(data) {
    let time = 0;
    const events = new Stream(data.stdout).map(e => {
      time += e[0];
      return [time, "o", e[1]];
    });
    return {
      cols: data.width,
      rows: data.height,
      events
    };
  }
  function parseAsciicastV2(header, events) {
    return {
      cols: header.width,
      rows: header.height,
      theme: parseTheme$1(header.theme),
      events,
      idleTimeLimit: header.idle_time_limit
    };
  }
  function parseAsciicastV3(header, events) {
    if (!(events instanceof Stream)) {
      events = new Stream(events);
    }
    let time = 0;
    events = events.map(e => {
      time += e[0];
      return [time, e[1], e[2]];
    });
    return {
      cols: header.term.cols,
      rows: header.term.rows,
      theme: parseTheme$1(header.term?.theme),
      events,
      idleTimeLimit: header.idle_time_limit
    };
  }
  function parseTheme$1(theme) {
    if (theme === undefined) return;
    const colorRegex = /^#[0-9A-Fa-f]{6}$/;
    const paletteRegex = /^(#[0-9A-Fa-f]{6}:){7,}#[0-9A-Fa-f]{6}$/;
    const fg = theme?.fg;
    const bg = theme?.bg;
    const palette = theme?.palette;
    if (colorRegex.test(fg) && colorRegex.test(bg) && paletteRegex.test(palette)) {
      return {
        foreground: fg,
        background: bg,
        palette: palette.split(":")
      };
    }
  }
  function unparseAsciicastV2(recording) {
    const header = JSON.stringify({
      version: 2,
      width: recording.cols,
      height: recording.rows
    });
    const events = recording.events.map(JSON.stringify).join("\n");
    return `${header}\n${events}\n`;
  }

  function recording(src, _ref, _ref2) {
    let {
      feed,
      resize,
      onInput,
      onMarker,
      now,
      setTimeout,
      setState,
      logger
    } = _ref;
    let {
      idleTimeLimit,
      startAt,
      loop,
      posterTime,
      markers: markers_,
      pauseOnMarkers,
      cols: initialCols,
      rows: initialRows
    } = _ref2;
    let cols;
    let rows;
    let events;
    let markers;
    let duration;
    let effectiveStartAt;
    let eventTimeoutId;
    let nextEventIndex = 0;
    let lastEventTime = 0;
    let startTime;
    let pauseElapsedTime;
    let playCount = 0;
    async function init() {
      const {
        parser,
        minFrameTime,
        inputOffset,
        dumpFilename,
        encoding = "utf-8"
      } = src;
      const recording = prepare(await parser(await doFetch(src), {
        encoding
      }), logger, {
        idleTimeLimit,
        startAt,
        minFrameTime,
        inputOffset,
        markers_
      });
      ({
        cols,
        rows,
        events,
        duration,
        effectiveStartAt
      } = recording);
      initialCols = initialCols ?? cols;
      initialRows = initialRows ?? rows;
      if (events.length === 0) {
        throw "recording is missing events";
      }
      if (dumpFilename !== undefined) {
        dump(recording, dumpFilename);
      }
      const poster = posterTime !== undefined ? getPoster(posterTime) : undefined;
      markers = events.filter(e => e[1] === "m").map(e => [e[0], e[2].label]);
      return {
        cols,
        rows,
        duration,
        theme: recording.theme,
        poster,
        markers
      };
    }
    function doFetch(_ref3) {
      let {
        url,
        data,
        fetchOpts = {}
      } = _ref3;
      if (typeof url === "string") {
        return doFetchOne(url, fetchOpts);
      } else if (Array.isArray(url)) {
        return Promise.all(url.map(url => doFetchOne(url, fetchOpts)));
      } else if (data !== undefined) {
        if (typeof data === "function") {
          data = data();
        }
        if (!(data instanceof Promise)) {
          data = Promise.resolve(data);
        }
        return data.then(value => {
          if (typeof value === "string" || value instanceof ArrayBuffer) {
            return new Response(value);
          } else {
            return value;
          }
        });
      } else {
        throw "failed fetching recording file: url/data missing in src";
      }
    }
    async function doFetchOne(url, fetchOpts) {
      const response = await fetch(url, fetchOpts);
      if (!response.ok) {
        throw `failed fetching recording from ${url}: ${response.status} ${response.statusText}`;
      }
      return response;
    }
    function delay(targetTime) {
      let delay = targetTime * 1000 - (now() - startTime);
      if (delay < 0) {
        delay = 0;
      }
      return delay;
    }
    function scheduleNextEvent() {
      const nextEvent = events[nextEventIndex];
      if (nextEvent) {
        eventTimeoutId = setTimeout(runNextEvent, delay(nextEvent[0]));
      } else {
        onEnd();
      }
    }
    function runNextEvent() {
      let event = events[nextEventIndex];
      let elapsedWallTime;
      do {
        lastEventTime = event[0];
        nextEventIndex++;
        const stop = executeEvent(event);
        if (stop) {
          return;
        }
        event = events[nextEventIndex];
        elapsedWallTime = now() - startTime;
      } while (event && elapsedWallTime > event[0] * 1000);
      scheduleNextEvent();
    }
    function cancelNextEvent() {
      clearTimeout(eventTimeoutId);
      eventTimeoutId = null;
    }
    function executeEvent(event) {
      const [time, type, data] = event;
      if (type === "o") {
        feed(data);
      } else if (type === "i") {
        onInput(data);
      } else if (type === "r") {
        const [cols, rows] = data.split("x");
        resize(cols, rows);
      } else if (type === "m") {
        onMarker(data);
        if (pauseOnMarkers) {
          pause();
          pauseElapsedTime = time * 1000;
          setState("idle", {
            reason: "paused"
          });
          return true;
        }
      }
      return false;
    }
    function onEnd() {
      cancelNextEvent();
      playCount++;
      if (loop === true || typeof loop === "number" && playCount < loop) {
        nextEventIndex = 0;
        startTime = now();
        feed("\x1bc"); // reset terminal
        resizeTerminalToInitialSize();
        scheduleNextEvent();
      } else {
        pauseElapsedTime = duration * 1000;
        setState("ended");
      }
    }
    function play() {
      if (eventTimeoutId) throw "already playing";
      if (events[nextEventIndex] === undefined) throw "already ended";
      if (effectiveStartAt !== null) {
        seek(effectiveStartAt);
      }
      resume();
      return true;
    }
    function pause() {
      if (!eventTimeoutId) return true;
      cancelNextEvent();
      pauseElapsedTime = now() - startTime;
      return true;
    }
    function resume() {
      startTime = now() - pauseElapsedTime;
      pauseElapsedTime = null;
      scheduleNextEvent();
    }
    function seek(where) {
      const isPlaying = !!eventTimeoutId;
      pause();
      const currentTime = (pauseElapsedTime ?? 0) / 1000;
      if (typeof where === "string") {
        if (where === "<<") {
          where = currentTime - 5;
        } else if (where === ">>") {
          where = currentTime + 5;
        } else if (where === "<<<") {
          where = currentTime - 0.1 * duration;
        } else if (where === ">>>") {
          where = currentTime + 0.1 * duration;
        } else if (where[where.length - 1] === "%") {
          where = parseFloat(where.substring(0, where.length - 1)) / 100 * duration;
        }
      } else if (typeof where === "object") {
        if (where.marker === "prev") {
          where = findMarkerTimeBefore(currentTime) ?? 0;
          if (isPlaying && currentTime - where < 1) {
            where = findMarkerTimeBefore(where) ?? 0;
          }
        } else if (where.marker === "next") {
          where = findMarkerTimeAfter(currentTime) ?? duration;
        } else if (typeof where.marker === "number") {
          const marker = markers[where.marker];
          if (marker === undefined) {
            throw `invalid marker index: ${where.marker}`;
          } else {
            where = marker[0];
          }
        }
      }
      const targetTime = Math.min(Math.max(where, 0), duration);
      if (targetTime < lastEventTime) {
        feed("\x1bc"); // reset terminal
        resizeTerminalToInitialSize();
        nextEventIndex = 0;
        lastEventTime = 0;
      }
      let event = events[nextEventIndex];
      while (event && event[0] <= targetTime) {
        if (event[1] === "o" || event[1] === "r") {
          executeEvent(event);
        }
        lastEventTime = event[0];
        event = events[++nextEventIndex];
      }
      pauseElapsedTime = targetTime * 1000;
      effectiveStartAt = null;
      if (isPlaying) {
        resume();
      }
      return true;
    }
    function findMarkerTimeBefore(time) {
      if (markers.length == 0) return;
      let i = 0;
      let marker = markers[i];
      let lastMarkerTimeBefore;
      while (marker && marker[0] < time) {
        lastMarkerTimeBefore = marker[0];
        marker = markers[++i];
      }
      return lastMarkerTimeBefore;
    }
    function findMarkerTimeAfter(time) {
      if (markers.length == 0) return;
      let i = markers.length - 1;
      let marker = markers[i];
      let firstMarkerTimeAfter;
      while (marker && marker[0] > time) {
        firstMarkerTimeAfter = marker[0];
        marker = markers[--i];
      }
      return firstMarkerTimeAfter;
    }
    function step(n) {
      if (n === undefined) {
        n = 1;
      }
      let nextEvent;
      let targetIndex;
      if (n > 0) {
        let index = nextEventIndex;
        nextEvent = events[index];
        for (let i = 0; i < n; i++) {
          while (nextEvent !== undefined && nextEvent[1] !== "o") {
            nextEvent = events[++index];
          }
          if (nextEvent !== undefined && nextEvent[1] === "o") {
            targetIndex = index;
          }
        }
      } else {
        let index = Math.max(nextEventIndex - 2, 0);
        nextEvent = events[index];
        for (let i = n; i < 0; i++) {
          while (nextEvent !== undefined && nextEvent[1] !== "o") {
            nextEvent = events[--index];
          }
          if (nextEvent !== undefined && nextEvent[1] === "o") {
            targetIndex = index;
          }
        }
        if (targetIndex !== undefined) {
          feed("\x1bc"); // reset terminal
          resizeTerminalToInitialSize();
          nextEventIndex = 0;
        }
      }
      if (targetIndex === undefined) return;
      while (nextEventIndex <= targetIndex) {
        nextEvent = events[nextEventIndex++];
        if (nextEvent[1] === "o" || nextEvent[1] === "r") {
          executeEvent(nextEvent);
        }
      }
      lastEventTime = nextEvent[0];
      pauseElapsedTime = lastEventTime * 1000;
      effectiveStartAt = null;
      if (events[targetIndex + 1] === undefined) {
        onEnd();
      }
    }
    function restart() {
      if (eventTimeoutId) throw "still playing";
      if (events[nextEventIndex] !== undefined) throw "not ended";
      seek(0);
      resume();
      return true;
    }
    function getPoster(time) {
      return events.filter(e => e[0] < time && e[1] === "o").map(e => e[2]);
    }
    function getCurrentTime() {
      if (eventTimeoutId) {
        return (now() - startTime) / 1000;
      } else {
        return (pauseElapsedTime ?? 0) / 1000;
      }
    }
    function resizeTerminalToInitialSize() {
      resize(initialCols, initialRows);
    }
    return {
      init,
      play,
      pause,
      seek,
      step,
      restart,
      stop: pause,
      getCurrentTime
    };
  }
  function batcher(logger) {
    let minFrameTime = arguments.length > 1 && arguments[1] !== undefined ? arguments[1] : 1.0 / 60;
    let prevEvent;
    return emit => {
      let ic = 0;
      let oc = 0;
      return {
        step: event => {
          ic++;
          if (prevEvent === undefined) {
            prevEvent = event;
            return;
          }
          if (event[1] === "o" && prevEvent[1] === "o" && event[0] - prevEvent[0] < minFrameTime) {
            prevEvent[2] += event[2];
          } else {
            emit(prevEvent);
            prevEvent = event;
            oc++;
          }
        },
        flush: () => {
          if (prevEvent !== undefined) {
            emit(prevEvent);
            oc++;
          }
          logger.debug(`batched ${ic} frames to ${oc} frames`);
        }
      };
    };
  }
  function prepare(recording, logger, _ref4) {
    let {
      startAt = 0,
      idleTimeLimit,
      minFrameTime,
      inputOffset,
      markers_
    } = _ref4;
    let {
      events
    } = recording;
    if (!(events instanceof Stream)) {
      events = new Stream(events);
    }
    idleTimeLimit = idleTimeLimit ?? recording.idleTimeLimit ?? Infinity;
    const limiterOutput = {
      offset: 0
    };
    events = events.transform(batcher(logger, minFrameTime)).map(timeLimiter(idleTimeLimit, startAt, limiterOutput)).map(markerWrapper());
    if (markers_ !== undefined) {
      markers_ = new Stream(markers_).map(normalizeMarker);
      events = events.filter(e => e[1] !== "m").multiplex(markers_, (a, b) => a[0] < b[0]).map(markerWrapper());
    }
    events = events.toArray();
    if (inputOffset !== undefined) {
      events = events.map(e => e[1] === "i" ? [e[0] + inputOffset, e[1], e[2]] : e);
      events.sort((a, b) => a[0] - b[0]);
    }
    const duration = events[events.length - 1][0];
    const effectiveStartAt = startAt - limiterOutput.offset;
    return {
      ...recording,
      events,
      duration,
      effectiveStartAt
    };
  }
  function normalizeMarker(m) {
    return typeof m === "number" ? [m, "m", ""] : [m[0], "m", m[1]];
  }
  function timeLimiter(idleTimeLimit, startAt, output) {
    let prevT = 0;
    let shift = 0;
    return function (e) {
      const delay = e[0] - prevT;
      const delta = delay - idleTimeLimit;
      prevT = e[0];
      if (delta > 0) {
        shift += delta;
        if (e[0] < startAt) {
          output.offset += delta;
        }
      }
      return [e[0] - shift, e[1], e[2]];
    };
  }
  function markerWrapper() {
    let i = 0;
    return function (e) {
      if (e[1] === "m") {
        return [e[0], e[1], {
          index: i++,
          time: e[0],
          label: e[2]
        }];
      } else {
        return e;
      }
    };
  }
  function dump(recording, filename) {
    const link = document.createElement("a");
    const events = recording.events.map(e => e[1] === "m" ? [e[0], e[1], e[2].label] : e);
    const asciicast = unparseAsciicastV2({
      ...recording,
      events
    });
    link.href = URL.createObjectURL(new Blob([asciicast], {
      type: "text/plain"
    }));
    link.download = filename;
    link.click();
  }

  function clock(_ref, _ref2, _ref3) {
    let {
      hourColor = 3,
      minuteColor = 4,
      separatorColor = 9
    } = _ref;
    let {
      feed
    } = _ref2;
    let {
      cols = 5,
      rows = 1
    } = _ref3;
    const middleRow = Math.floor(rows / 2);
    const leftPad = Math.floor(cols / 2) - 2;
    const setupCursor = `\x1b[?25l\x1b[1m\x1b[${middleRow}B`;
    let intervalId;
    const getCurrentTime = () => {
      const d = new Date();
      const h = d.getHours();
      const m = d.getMinutes();
      const seqs = [];
      seqs.push("\r");
      for (let i = 0; i < leftPad; i++) {
        seqs.push(" ");
      }
      seqs.push(`\x1b[3${hourColor}m`);
      if (h < 10) {
        seqs.push("0");
      }
      seqs.push(`${h}`);
      seqs.push(`\x1b[3${separatorColor};5m:\x1b[25m`);
      seqs.push(`\x1b[3${minuteColor}m`);
      if (m < 10) {
        seqs.push("0");
      }
      seqs.push(`${m}`);
      return seqs;
    };
    const updateTime = () => {
      getCurrentTime().forEach(feed);
    };
    return {
      init: () => {
        const duration = 24 * 60;
        const poster = [setupCursor].concat(getCurrentTime());
        return {
          cols,
          rows,
          duration,
          poster
        };
      },
      play: () => {
        feed(setupCursor);
        updateTime();
        intervalId = setInterval(updateTime, 1000);
        return true;
      },
      stop: () => {
        clearInterval(intervalId);
      },
      getCurrentTime: () => {
        const d = new Date();
        return d.getHours() * 60 + d.getMinutes();
      }
    };
  }

  function random(src, _ref) {
    let {
      feed,
      setTimeout
    } = _ref;
    const base = " ".charCodeAt(0);
    const range = "~".charCodeAt(0) - base;
    let timeoutId;
    const schedule = () => {
      const t = Math.pow(5, Math.random() * 4);
      timeoutId = setTimeout(print, t);
    };
    const print = () => {
      schedule();
      const char = String.fromCharCode(base + Math.floor(Math.random() * range));
      feed(char);
    };
    return () => {
      schedule();
      return () => clearInterval(timeoutId);
    };
  }

  function benchmark(_ref, _ref2) {
    let {
      url,
      iterations = 10
    } = _ref;
    let {
      feed,
      setState,
      now
    } = _ref2;
    let data;
    let byteCount = 0;
    return {
      async init() {
        const recording = await parse$2(await fetch(url));
        const {
          cols,
          rows,
          events
        } = recording;
        data = Array.from(events).filter(_ref3 => {
          let [_time, type, _text] = _ref3;
          return type === "o";
        }).map(_ref4 => {
          let [time, _type, text] = _ref4;
          return [time, text];
        });
        const duration = data[data.length - 1][0];
        for (const [_, text] of data) {
          byteCount += new Blob([text]).size;
        }
        return {
          cols,
          rows,
          duration
        };
      },
      play() {
        const startTime = now();
        for (let i = 0; i < iterations; i++) {
          for (const [_, text] of data) {
            feed(text);
          }
          feed("\x1bc"); // reset terminal
        }

        const endTime = now();
        const duration = (endTime - startTime) / 1000;
        const throughput = byteCount * iterations / duration;
        const throughputMbs = byteCount / (1024 * 1024) * iterations / duration;
        console.info("benchmark: result", {
          byteCount,
          iterations,
          duration,
          throughput,
          throughputMbs
        });
        setTimeout(() => {
          setState("stopped", {
            reason: "ended"
          });
        }, 0);
        return true;
      }
    };
  }

  class Queue {
    constructor() {
      this.items = [];
      this.onPush = undefined;
    }
    push(item) {
      this.items.push(item);
      if (this.onPush !== undefined) {
        this.onPush(this.popAll());
        this.onPush = undefined;
      }
    }
    popAll() {
      if (this.items.length > 0) {
        const items = this.items;
        this.items = [];
        return items;
      } else {
        const thiz = this;
        return new Promise(resolve => {
          thiz.onPush = resolve;
        });
      }
    }
  }

  function getBuffer(bufferTime, feed, resize, onInput, onMarker, setTime, baseStreamTime, minFrameTime, logger) {
    const execute = executeEvent(feed, resize, onInput, onMarker);
    if (bufferTime === 0) {
      logger.debug("using no buffer");
      return nullBuffer(execute);
    } else {
      bufferTime = bufferTime ?? {};
      let getBufferTime;
      if (typeof bufferTime === "number") {
        logger.debug(`using fixed time buffer (${bufferTime} ms)`);
        getBufferTime = _latency => bufferTime;
      } else if (typeof bufferTime === "function") {
        logger.debug("using custom dynamic buffer");
        getBufferTime = bufferTime({
          logger
        });
      } else {
        logger.debug("using adaptive buffer", bufferTime);
        getBufferTime = adaptiveBufferTimeProvider({
          logger
        }, bufferTime);
      }
      return buffer(getBufferTime, execute, setTime, logger, baseStreamTime ?? 0.0, minFrameTime);
    }
  }
  function nullBuffer(execute) {
    return {
      pushEvent(event) {
        execute(event[1], event[2]);
      },
      pushText(text) {
        execute("o", text);
      },
      stop() {}
    };
  }
  function executeEvent(feed, resize, onInput, onMarker) {
    return function (code, data) {
      if (code === "o") {
        feed(data);
      } else if (code === "i") {
        onInput(data);
      } else if (code === "r") {
        resize(data.cols, data.rows);
      } else if (code === "m") {
        onMarker(data);
      }
    };
  }
  function buffer(getBufferTime, execute, setTime, logger, baseStreamTime) {
    let minFrameTime = arguments.length > 5 && arguments[5] !== undefined ? arguments[5] : 1.0 / 60;
    let epoch = performance.now() - baseStreamTime * 1000;
    let bufferTime = getBufferTime(0);
    const queue = new Queue();
    minFrameTime *= 1000;
    let prevElapsedStreamTime = -minFrameTime;
    let stop = false;
    function elapsedWallTime() {
      return performance.now() - epoch;
    }
    setTimeout(async () => {
      while (!stop) {
        const events = await queue.popAll();
        if (stop) return;
        for (const event of events) {
          const elapsedStreamTime = event[0] * 1000 + bufferTime;
          if (elapsedStreamTime - prevElapsedStreamTime < minFrameTime) {
            execute(event[1], event[2]);
            continue;
          }
          const delay = elapsedStreamTime - elapsedWallTime();
          if (delay > 0) {
            await sleep(delay);
            if (stop) return;
          }
          setTime(event[0]);
          execute(event[1], event[2]);
          prevElapsedStreamTime = elapsedStreamTime;
        }
      }
    }, 0);
    return {
      pushEvent(event) {
        let latency = elapsedWallTime() - event[0] * 1000;
        if (latency < 0) {
          logger.debug(`correcting epoch by ${latency} ms`);
          epoch += latency;
          latency = 0;
        }
        bufferTime = getBufferTime(latency);
        queue.push(event);
      },
      pushText(text) {
        queue.push([elapsedWallTime() / 1000, "o", text]);
      },
      stop() {
        stop = true;
        queue.push(undefined);
      }
    };
  }
  function sleep(t) {
    return new Promise(resolve => {
      setTimeout(resolve, t);
    });
  }
  function adaptiveBufferTimeProvider(_ref, _ref2) {
    let {
      logger
    } = _ref;
    let {
      minTime = 25,
      maxLevel = 100,
      interval = 50,
      windowSize = 20,
      smoothingFactor = 0.2,
      minImprovementDuration = 1000
    } = _ref2;
    let bufferLevel = 0;
    let bufferTime = calcBufferTime(bufferLevel);
    let latencies = [];
    let maxJitter = 0;
    let jitterRange = 0;
    let improvementTs = null;
    function calcBufferTime(level) {
      if (level === 0) {
        return minTime;
      } else {
        return interval * level;
      }
    }
    return latency => {
      latencies.push(latency);
      if (latencies.length < windowSize) {
        return bufferTime;
      }
      latencies = latencies.slice(-windowSize);
      const currentMinJitter = min(latencies);
      const currentMaxJitter = max(latencies);
      const currentJitterRange = currentMaxJitter - currentMinJitter;
      maxJitter = currentMaxJitter * smoothingFactor + maxJitter * (1 - smoothingFactor);
      jitterRange = currentJitterRange * smoothingFactor + jitterRange * (1 - smoothingFactor);
      const minBufferTime = maxJitter + jitterRange;
      if (latency > bufferTime) {
        logger.debug('buffer underrun', {
          latency,
          maxJitter,
          jitterRange,
          bufferTime
        });
      }
      if (bufferLevel < maxLevel && minBufferTime > bufferTime) {
        bufferTime = calcBufferTime(bufferLevel += 1);
        logger.debug(`jitter increased, raising bufferTime`, {
          latency,
          maxJitter,
          jitterRange,
          bufferTime
        });
      } else if (bufferLevel > 1 && minBufferTime < calcBufferTime(bufferLevel - 2) || bufferLevel == 1 && minBufferTime < calcBufferTime(bufferLevel - 1)) {
        if (improvementTs === null) {
          improvementTs = performance.now();
        } else if (performance.now() - improvementTs > minImprovementDuration) {
          improvementTs = performance.now();
          bufferTime = calcBufferTime(bufferLevel -= 1);
          logger.debug(`jitter decreased, lowering bufferTime`, {
            latency,
            maxJitter,
            jitterRange,
            bufferTime
          });
        }
        return bufferTime;
      }
      improvementTs = null;
      return bufferTime;
    };
  }
  function min(numbers) {
    return numbers.reduce((prev, cur) => cur < prev ? cur : prev);
  }
  function max(numbers) {
    return numbers.reduce((prev, cur) => cur > prev ? cur : prev);
  }

  const ONE_SEC_IN_USEC = 1000000;
  function alisHandler(logger) {
    const outputDecoder = new TextDecoder();
    const inputDecoder = new TextDecoder();
    let handler = parseMagicString;
    let lastEventTime;
    let markerIndex = 0;
    function parseMagicString(buffer) {
      const text = new TextDecoder().decode(buffer);
      if (text === "ALiS\x01") {
        handler = parseFirstFrame;
      } else {
        throw "not an ALiS v1 live stream";
      }
    }
    function parseFirstFrame(buffer) {
      const view = new BinaryReader(new DataView(buffer));
      const type = view.getUint8();
      if (type !== 0x01) throw `expected reset (0x01) frame, got ${type}`;
      return parseResetFrame(view, buffer);
    }
    function parseResetFrame(view, buffer) {
      view.decodeVarUint();
      let time = view.decodeVarUint();
      lastEventTime = time;
      time = time / ONE_SEC_IN_USEC;
      markerIndex = 0;
      const cols = view.decodeVarUint();
      const rows = view.decodeVarUint();
      const themeFormat = view.getUint8();
      let theme;
      if (themeFormat === 8) {
        const len = (2 + 8) * 3;
        theme = parseTheme(new Uint8Array(buffer, view.offset, len));
        view.forward(len);
      } else if (themeFormat === 16) {
        const len = (2 + 16) * 3;
        theme = parseTheme(new Uint8Array(buffer, view.offset, len));
        view.forward(len);
      } else if (themeFormat !== 0) {
        throw `alis: invalid theme format (${themeFormat})`;
      }
      const initLen = view.decodeVarUint();
      let init;
      if (initLen > 0) {
        init = outputDecoder.decode(new Uint8Array(buffer, view.offset, initLen));
      }
      handler = parseFrame;
      return {
        time,
        term: {
          size: {
            cols,
            rows
          },
          theme,
          init
        }
      };
    }
    function parseFrame(buffer) {
      const view = new BinaryReader(new DataView(buffer));
      const type = view.getUint8();
      if (type === 0x01) {
        return parseResetFrame(view, buffer);
      } else if (type === 0x6f) {
        return parseOutputFrame(view, buffer);
      } else if (type === 0x69) {
        return parseInputFrame(view, buffer);
      } else if (type === 0x72) {
        return parseResizeFrame(view);
      } else if (type === 0x6d) {
        return parseMarkerFrame(view, buffer);
      } else if (type === 0x04) {
        // EOT
        handler = parseFirstFrame;
        return false;
      } else {
        logger.debug(`alis: unknown frame type: ${type}`);
      }
    }
    function parseOutputFrame(view, buffer) {
      view.decodeVarUint();
      const relTime = view.decodeVarUint();
      lastEventTime += relTime;
      const len = view.decodeVarUint();
      const text = outputDecoder.decode(new Uint8Array(buffer, view.offset, len));
      return [lastEventTime / ONE_SEC_IN_USEC, "o", text];
    }
    function parseInputFrame(view, buffer) {
      view.decodeVarUint();
      const relTime = view.decodeVarUint();
      lastEventTime += relTime;
      const len = view.decodeVarUint();
      const text = inputDecoder.decode(new Uint8Array(buffer, view.offset, len));
      return [lastEventTime / ONE_SEC_IN_USEC, "i", text];
    }
    function parseResizeFrame(view) {
      view.decodeVarUint();
      const relTime = view.decodeVarUint();
      lastEventTime += relTime;
      const cols = view.decodeVarUint();
      const rows = view.decodeVarUint();
      return [lastEventTime / ONE_SEC_IN_USEC, "r", {
        cols,
        rows
      }];
    }
    function parseMarkerFrame(view, buffer) {
      view.decodeVarUint();
      const relTime = view.decodeVarUint();
      lastEventTime += relTime;
      const len = view.decodeVarUint();
      const decoder = new TextDecoder();
      const index = markerIndex++;
      const time = lastEventTime / ONE_SEC_IN_USEC;
      const label = decoder.decode(new Uint8Array(buffer, view.offset, len));
      return [time, "m", {
        index,
        time,
        label
      }];
    }
    return function (buffer) {
      return handler(buffer);
    };
  }
  function parseTheme(arr) {
    const colorCount = arr.length / 3;
    const foreground = hexColor(arr[0], arr[1], arr[2]);
    const background = hexColor(arr[3], arr[4], arr[5]);
    const palette = [];
    for (let i = 2; i < colorCount; i++) {
      palette.push(hexColor(arr[i * 3], arr[i * 3 + 1], arr[i * 3 + 2]));
    }
    return {
      foreground,
      background,
      palette
    };
  }
  function hexColor(r, g, b) {
    return `#${byteToHex(r)}${byteToHex(g)}${byteToHex(b)}`;
  }
  function byteToHex(value) {
    return value.toString(16).padStart(2, "0");
  }
  class BinaryReader {
    constructor(inner) {
      let offset = arguments.length > 1 && arguments[1] !== undefined ? arguments[1] : 0;
      this.inner = inner;
      this.offset = offset;
    }
    forward(delta) {
      this.offset += delta;
    }
    getUint8() {
      const value = this.inner.getUint8(this.offset);
      this.offset += 1;
      return value;
    }
    decodeVarUint() {
      let number = BigInt(0);
      let shift = BigInt(0);
      let byte = this.getUint8();
      while (byte > 127) {
        byte &= 127;
        number += BigInt(byte) << shift;
        shift += BigInt(7);
        byte = this.getUint8();
      }
      number = number + (BigInt(byte) << shift);
      return Number(number);
    }
  }

  function jsonHandler() {
    let parse = parseHeader;
    function parseHeader(buffer) {
      const header = JSON.parse(buffer);
      if (header.version !== 2) {
        throw "not an asciicast v2 stream";
      }
      parse = parseEvent;
      return {
        time: 0.0,
        term: {
          size: {
            cols: header.width,
            rows: header.height
          }
        }
      };
    }
    function parseEvent(buffer) {
      const event = JSON.parse(buffer);
      if (event[1] === "r") {
        const [cols, rows] = event[2].split("x");
        return [event[0], "r", {
          cols,
          rows
        }];
      } else {
        return event;
      }
    }
    return function (buffer) {
      return parse(buffer);
    };
  }

  function rawHandler() {
    const outputDecoder = new TextDecoder();
    let parse = parseSize;
    function parseSize(buffer) {
      const text = outputDecoder.decode(buffer, {
        stream: true
      });
      const [cols, rows] = sizeFromResizeSeq(text) ?? sizeFromScriptStartMessage(text) ?? [80, 24];
      parse = parseOutput;
      return {
        time: 0.0,
        term: {
          size: {
            cols,
            rows
          },
          init: text
        }
      };
    }
    function parseOutput(buffer) {
      return outputDecoder.decode(buffer, {
        stream: true
      });
    }
    return function (buffer) {
      return parse(buffer);
    };
  }
  function sizeFromResizeSeq(text) {
    const match = text.match(/\x1b\[8;(\d+);(\d+)t/);
    if (match !== null) {
      return [parseInt(match[2], 10), parseInt(match[1], 10)];
    }
  }
  function sizeFromScriptStartMessage(text) {
    const match = text.match(/\[.*COLUMNS="(\d{1,3})" LINES="(\d{1,3})".*\]/);
    if (match !== null) {
      return [parseInt(match[1], 10), parseInt(match[2], 10)];
    }
  }

  function exponentialDelay(attempt) {
    return Math.min(500 * Math.pow(2, attempt), 5000);
  }
  function websocket(_ref, _ref2) {
    let {
      url,
      bufferTime,
      reconnectDelay = exponentialDelay,
      minFrameTime
    } = _ref;
    let {
      feed,
      reset,
      resize,
      onInput,
      onMarker,
      setState,
      logger
    } = _ref2;
    logger = new PrefixedLogger(logger, "websocket: ");
    let socket;
    let buf;
    let clock = new NullClock();
    let reconnectAttempt = 0;
    let successfulConnectionTimeout;
    let stop = false;
    let wasOnline = false;
    let initTimeout;
    function connect() {
      socket = new WebSocket(url, ["v1.alis", "v2.asciicast", "raw"]);
      socket.binaryType = "arraybuffer";
      socket.onopen = () => {
        const proto = socket.protocol || "raw";
        logger.info("opened");
        logger.info(`activating ${proto} protocol handler`);
        if (proto === "v1.alis") {
          socket.onmessage = onMessage(alisHandler(logger));
        } else if (proto === "v2.asciicast") {
          socket.onmessage = onMessage(jsonHandler());
        } else if (proto === "raw") {
          socket.onmessage = onMessage(rawHandler());
        }
        successfulConnectionTimeout = setTimeout(() => {
          reconnectAttempt = 0;
        }, 1000);
      };
      socket.onclose = event => {
        clearTimeout(initTimeout);
        stopBuffer();
        if (stop || event.code === 1000 || event.code === 1005) {
          logger.info("closed");
          setState("ended", {
            message: "Stream ended"
          });
        } else if (event.code === 1002) {
          logger.debug(`close reason: ${event.reason}`);
          setState("ended", {
            message: "Err: Player not compatible with the server"
          });
        } else {
          clearTimeout(successfulConnectionTimeout);
          const delay = reconnectDelay(reconnectAttempt++);
          logger.info(`unclean close, reconnecting in ${delay}...`);
          setState("loading");
          setTimeout(connect, delay);
        }
      };
      wasOnline = false;
    }
    function onMessage(handler) {
      initTimeout = setTimeout(onStreamEnd, 5000);
      return function (event) {
        try {
          const result = handler(event.data);
          if (buf) {
            if (Array.isArray(result)) {
              buf.pushEvent(result);
            } else if (typeof result === "string") {
              buf.pushText(result);
            } else if (typeof result === "object" && !Array.isArray(result)) {
              // TODO: check last event ID from the parser, don't reset if we didn't miss anything
              onStreamReset(result);
            } else if (result === false) {
              // EOT
              onStreamEnd();
            } else if (result !== undefined) {
              throw `unexpected value from protocol handler: ${result}`;
            }
          } else {
            if (typeof result === "object" && !Array.isArray(result)) {
              onStreamReset(result);
              clearTimeout(initTimeout);
            } else if (result === undefined) {
              clearTimeout(initTimeout);
              initTimeout = setTimeout(onStreamEnd, 1000);
            } else {
              clearTimeout(initTimeout);
              throw `unexpected value from protocol handler: ${result}`;
            }
          }
        } catch (e) {
          socket.close();
          throw e;
        }
      };
    }
    function onStreamReset(_ref3) {
      let {
        time,
        term
      } = _ref3;
      const {
        size,
        init,
        theme
      } = term;
      const {
        cols,
        rows
      } = size;
      logger.info(`stream reset (${cols}x${rows} @${time})`);
      setState("playing");
      stopBuffer();
      buf = getBuffer(bufferTime, feed, resize, onInput, onMarker, t => clock.setTime(t), time, minFrameTime, logger);
      reset(cols, rows, init, theme);
      clock = new Clock();
      wasOnline = true;
      if (typeof time === "number") {
        clock.setTime(time);
      }
    }
    function onStreamEnd() {
      stopBuffer();
      if (wasOnline) {
        logger.info("stream ended");
        setState("offline", {
          message: "Stream ended"
        });
      } else {
        logger.info("stream offline");
        setState("offline", {
          message: "Stream offline"
        });
      }
      clock = new NullClock();
    }
    function stopBuffer() {
      if (buf) buf.stop();
      buf = null;
    }
    return {
      play: () => {
        connect();
      },
      stop: () => {
        stop = true;
        stopBuffer();
        if (socket !== undefined) socket.close();
      },
      getCurrentTime: () => clock.getTime()
    };
  }

  function eventsource(_ref, _ref2) {
    let {
      url,
      bufferTime,
      minFrameTime
    } = _ref;
    let {
      feed,
      reset,
      resize,
      onInput,
      onMarker,
      setState,
      logger
    } = _ref2;
    logger = new PrefixedLogger(logger, "eventsource: ");
    let es;
    let buf;
    let clock = new NullClock();
    function initBuffer(baseStreamTime) {
      if (buf !== undefined) buf.stop();
      buf = getBuffer(bufferTime, feed, resize, onInput, onMarker, t => clock.setTime(t), baseStreamTime, minFrameTime, logger);
    }
    return {
      play: () => {
        es = new EventSource(url);
        es.addEventListener("open", () => {
          logger.info("opened");
          initBuffer();
        });
        es.addEventListener("error", e => {
          logger.info("errored");
          logger.debug({
            e
          });
          setState("loading");
        });
        es.addEventListener("message", event => {
          const e = JSON.parse(event.data);
          if (Array.isArray(e)) {
            buf.pushEvent(e);
          } else if (e.cols !== undefined || e.width !== undefined) {
            const cols = e.cols ?? e.width;
            const rows = e.rows ?? e.height;
            logger.debug(`vt reset (${cols}x${rows})`);
            setState("playing");
            initBuffer(e.time);
            reset(cols, rows, e.init ?? undefined);
            clock = new Clock();
            if (typeof e.time === "number") {
              clock.setTime(e.time);
            }
          } else if (e.state === "offline") {
            logger.info("stream offline");
            setState("offline", {
              message: "Stream offline"
            });
            clock = new NullClock();
          }
        });
        es.addEventListener("done", () => {
          logger.info("closed");
          es.close();
          setState("ended", {
            message: "Stream ended"
          });
        });
      },
      stop: () => {
        if (buf !== undefined) buf.stop();
        if (es !== undefined) es.close();
      },
      getCurrentTime: () => clock.getTime()
    };
  }

  async function parse$1(responses, _ref) {
    let {
      encoding
    } = _ref;
    const textDecoder = new TextDecoder(encoding);
    let cols;
    let rows;
    let timing = (await responses[0].text()).split("\n").filter(line => line.length > 0).map(line => line.split(" "));
    if (timing[0].length < 3) {
      timing = timing.map(entry => ["O", entry[0], entry[1]]);
    }
    const buffer = await responses[1].arrayBuffer();
    const array = new Uint8Array(buffer);
    const dataOffset = array.findIndex(byte => byte == 0x0a) + 1;
    const header = textDecoder.decode(array.subarray(0, dataOffset));
    const sizeMatch = header.match(/COLUMNS="(\d+)" LINES="(\d+)"/);
    if (sizeMatch !== null) {
      cols = parseInt(sizeMatch[1], 10);
      rows = parseInt(sizeMatch[2], 10);
    }
    const stdout = {
      array,
      cursor: dataOffset
    };
    let stdin = stdout;
    if (responses[2] !== undefined) {
      const buffer = await responses[2].arrayBuffer();
      const array = new Uint8Array(buffer);
      stdin = {
        array,
        cursor: dataOffset
      };
    }
    const events = [];
    let time = 0;
    for (const entry of timing) {
      time += parseFloat(entry[1]);
      if (entry[0] === "O") {
        const count = parseInt(entry[2], 10);
        const bytes = stdout.array.subarray(stdout.cursor, stdout.cursor + count);
        const text = textDecoder.decode(bytes);
        events.push([time, "o", text]);
        stdout.cursor += count;
      } else if (entry[0] === "I") {
        const count = parseInt(entry[2], 10);
        const bytes = stdin.array.subarray(stdin.cursor, stdin.cursor + count);
        const text = textDecoder.decode(bytes);
        events.push([time, "i", text]);
        stdin.cursor += count;
      } else if (entry[0] === "S" && entry[2] === "SIGWINCH") {
        const cols = parseInt(entry[4].slice(5), 10);
        const rows = parseInt(entry[3].slice(5), 10);
        events.push([time, "r", `${cols}x${rows}`]);
      } else if (entry[0] === "H" && entry[2] === "COLUMNS") {
        cols = parseInt(entry[3], 10);
      } else if (entry[0] === "H" && entry[2] === "LINES") {
        rows = parseInt(entry[3], 10);
      }
    }
    cols = cols ?? 80;
    rows = rows ?? 24;
    return {
      cols,
      rows,
      events
    };
  }

  async function parse(response, _ref) {
    let {
      encoding
    } = _ref;
    const textDecoder = new TextDecoder(encoding);
    const buffer = await response.arrayBuffer();
    const array = new Uint8Array(buffer);
    const firstFrame = parseFrame(array);
    const baseTime = firstFrame.time;
    const firstFrameText = textDecoder.decode(firstFrame.data);
    const sizeMatch = firstFrameText.match(/\x1b\[8;(\d+);(\d+)t/);
    const events = [];
    let cols = 80;
    let rows = 24;
    if (sizeMatch !== null) {
      cols = parseInt(sizeMatch[2], 10);
      rows = parseInt(sizeMatch[1], 10);
    }
    let cursor = 0;
    let frame = parseFrame(array);
    while (frame !== undefined) {
      const time = frame.time - baseTime;
      const text = textDecoder.decode(frame.data);
      events.push([time, "o", text]);
      cursor += frame.len;
      frame = parseFrame(array.subarray(cursor));
    }
    return {
      cols,
      rows,
      events
    };
  }
  function parseFrame(array) {
    if (array.length < 13) return;
    const time = parseTimestamp(array.subarray(0, 8));
    const len = parseNumber(array.subarray(8, 12));
    const data = array.subarray(12, 12 + len);
    return {
      time,
      data,
      len: len + 12
    };
  }
  function parseNumber(array) {
    return array[0] + array[1] * 256 + array[2] * 256 * 256 + array[3] * 256 * 256 * 256;
  }
  function parseTimestamp(array) {
    const sec = parseNumber(array.subarray(0, 4));
    const usec = parseNumber(array.subarray(4, 8));
    return sec + usec / 1000000;
  }

  const vt = loadVt(); // trigger async loading of wasm

  class State {
    constructor(core) {
      this.core = core;
      this.driver = core.driver;
    }
    onEnter(data) {}
    init() {}
    play() {}
    pause() {}
    togglePlay() {}
    seek(where) {
      return false;
    }
    step(n) {}
    stop() {
      this.driver.stop();
    }
  }
  class UninitializedState extends State {
    async init() {
      try {
        await this.core._initializeDriver();
        return this.core._setState("idle");
      } catch (e) {
        this.core._setState("errored");
        throw e;
      }
    }
    async play() {
      this.core._dispatchEvent("play");
      const idleState = await this.init();
      await idleState.doPlay();
    }
    async togglePlay() {
      await this.play();
    }
    async seek(where) {
      const idleState = await this.init();
      return await idleState.seek(where);
    }
    async step(n) {
      const idleState = await this.init();
      await idleState.step(n);
    }
    stop() {}
  }
  class Idle extends State {
    onEnter(_ref) {
      let {
        reason,
        message
      } = _ref;
      this.core._dispatchEvent("idle", {
        message
      });
      if (reason === "paused") {
        this.core._dispatchEvent("pause");
      }
    }
    async play() {
      this.core._dispatchEvent("play");
      await this.doPlay();
    }
    async doPlay() {
      const stop = await this.driver.play();
      if (stop === true) {
        this.core._setState("playing");
      } else if (typeof stop === "function") {
        this.core._setState("playing");
        this.driver.stop = stop;
      }
    }
    async togglePlay() {
      await this.play();
    }
    seek(where) {
      return this.driver.seek(where);
    }
    step(n) {
      this.driver.step(n);
    }
  }
  class PlayingState extends State {
    onEnter() {
      this.core._dispatchEvent("playing");
    }
    pause() {
      if (this.driver.pause() === true) {
        this.core._setState("idle", {
          reason: "paused"
        });
      }
    }
    togglePlay() {
      this.pause();
    }
    seek(where) {
      return this.driver.seek(where);
    }
  }
  class LoadingState extends State {
    onEnter() {
      this.core._dispatchEvent("loading");
    }
  }
  class OfflineState extends State {
    onEnter(_ref2) {
      let {
        message
      } = _ref2;
      this.core._dispatchEvent("offline", {
        message
      });
    }
  }
  class EndedState extends State {
    onEnter(_ref3) {
      let {
        message
      } = _ref3;
      this.core._dispatchEvent("ended", {
        message
      });
    }
    async play() {
      this.core._dispatchEvent("play");
      if (await this.driver.restart()) {
        this.core._setState('playing');
      }
    }
    async togglePlay() {
      await this.play();
    }
    seek(where) {
      if (this.driver.seek(where) === true) {
        this.core._setState('idle');
        return true;
      }
      return false;
    }
  }
  class ErroredState extends State {
    onEnter() {
      this.core._dispatchEvent("errored");
    }
  }
  class Core {
    constructor(src, opts) {
      this.logger = opts.logger;
      this.state = new UninitializedState(this);
      this.stateName = "uninitialized";
      this.driver = getDriver(src);
      this.changedLines = new Set();
      this.cursor = undefined;
      this.duration = undefined;
      this.cols = opts.cols;
      this.rows = opts.rows;
      this.speed = opts.speed;
      this.loop = opts.loop;
      this.autoPlay = opts.autoPlay;
      this.idleTimeLimit = opts.idleTimeLimit;
      this.preload = opts.preload;
      this.startAt = parseNpt(opts.startAt);
      this.poster = this._parsePoster(opts.poster);
      this.markers = this._normalizeMarkers(opts.markers);
      this.pauseOnMarkers = opts.pauseOnMarkers;
      this.commandQueue = Promise.resolve();
      this.eventHandlers = new Map([["ended", []], ["errored", []], ["idle", []], ["input", []], ["loading", []], ["marker", []], ["metadata", []], ["offline", []], ["pause", []], ["play", []], ["playing", []], ["ready", []], ["reset", []], ["resize", []], ["seeked", []], ["terminalUpdate", []]]);
    }
    async init() {
      this.wasm = await vt;
      const feed = this._feed.bind(this);
      const onInput = data => {
        this._dispatchEvent("input", {
          data
        });
      };
      const onMarker = _ref4 => {
        let {
          index,
          time,
          label
        } = _ref4;
        this._dispatchEvent("marker", {
          index,
          time,
          label
        });
      };
      const now = this._now.bind(this);
      const reset = this._resetVt.bind(this);
      const resize = this._resizeVt.bind(this);
      const setState = this._setState.bind(this);
      const posterTime = this.poster.type === "npt" ? this.poster.value : undefined;
      this.driver = this.driver({
        feed,
        onInput,
        onMarker,
        reset,
        resize,
        now,
        setTimeout: (f, t) => setTimeout(f, t / this.speed),
        setInterval: (f, t) => setInterval(f, t / this.speed),
        setState,
        logger: this.logger
      }, {
        cols: this.cols,
        rows: this.rows,
        idleTimeLimit: this.idleTimeLimit,
        startAt: this.startAt,
        loop: this.loop,
        posterTime: posterTime,
        markers: this.markers,
        pauseOnMarkers: this.pauseOnMarkers
      });
      if (typeof this.driver === "function") {
        this.driver = {
          play: this.driver
        };
      }
      if (this.preload || posterTime !== undefined) {
        this._withState(state => state.init());
      }
      const poster = this.poster.type === "text" ? this._renderPoster(this.poster.value) : null;
      const config = {
        isPausable: !!this.driver.pause,
        isSeekable: !!this.driver.seek,
        poster
      };
      if (this.driver.init === undefined) {
        this.driver.init = () => {
          return {};
        };
      }
      if (this.driver.pause === undefined) {
        this.driver.pause = () => {};
      }
      if (this.driver.seek === undefined) {
        this.driver.seek = where => false;
      }
      if (this.driver.step === undefined) {
        this.driver.step = n => {};
      }
      if (this.driver.stop === undefined) {
        this.driver.stop = () => {};
      }
      if (this.driver.restart === undefined) {
        this.driver.restart = () => {};
      }
      if (this.driver.getCurrentTime === undefined) {
        const play = this.driver.play;
        let clock = new NullClock();
        this.driver.play = () => {
          clock = new Clock(this.speed);
          return play();
        };
        this.driver.getCurrentTime = () => clock.getTime();
      }
      this._dispatchEvent("ready", config);
      if (this.autoPlay) {
        this.play();
      }
    }
    play() {
      return this._withState(state => state.play());
    }
    pause() {
      return this._withState(state => state.pause());
    }
    togglePlay() {
      return this._withState(state => state.togglePlay());
    }
    seek(where) {
      return this._withState(async state => {
        if (await state.seek(where)) {
          this._dispatchEvent("seeked");
        }
      });
    }
    step(n) {
      return this._withState(state => state.step(n));
    }
    stop() {
      return this._withState(state => state.stop());
    }
    getChanges() {
      const changes = {};
      if (this.changedLines.size > 0) {
        const lines = new Map();
        const rows = this.vt.rows;
        for (const i of this.changedLines) {
          if (i < rows) {
            lines.set(i, {
              id: i,
              segments: this.vt.getLine(i)
            });
          }
        }
        this.changedLines.clear();
        changes.lines = lines;
      }
      if (this.cursor === undefined && this.vt) {
        this.cursor = this.vt.getCursor() ?? false;
        changes.cursor = this.cursor;
      }
      return changes;
    }
    getCurrentTime() {
      return this.driver.getCurrentTime();
    }
    getRemainingTime() {
      if (typeof this.duration === "number") {
        return this.duration - Math.min(this.getCurrentTime(), this.duration);
      }
    }
    getProgress() {
      if (typeof this.duration === "number") {
        return Math.min(this.getCurrentTime(), this.duration) / this.duration;
      }
    }
    getDuration() {
      return this.duration;
    }
    addEventListener(eventName, handler) {
      this.eventHandlers.get(eventName).push(handler);
    }
    _dispatchEvent(eventName) {
      let data = arguments.length > 1 && arguments[1] !== undefined ? arguments[1] : {};
      for (const h of this.eventHandlers.get(eventName)) {
        h(data);
      }
    }
    _withState(f) {
      return this._enqueueCommand(() => f(this.state));
    }
    _enqueueCommand(f) {
      this.commandQueue = this.commandQueue.then(f);
      return this.commandQueue;
    }
    _setState(newState) {
      let data = arguments.length > 1 && arguments[1] !== undefined ? arguments[1] : {};
      if (this.stateName === newState) return this.state;
      this.stateName = newState;
      if (newState === "playing") {
        this.state = new PlayingState(this);
      } else if (newState === "idle") {
        this.state = new Idle(this);
      } else if (newState === "loading") {
        this.state = new LoadingState(this);
      } else if (newState === "ended") {
        this.state = new EndedState(this);
      } else if (newState === "offline") {
        this.state = new OfflineState(this);
      } else if (newState === "errored") {
        this.state = new ErroredState(this);
      } else {
        throw `invalid state: ${newState}`;
      }
      this.state.onEnter(data);
      return this.state;
    }
    _feed(data) {
      this._doFeed(data);
      this._dispatchEvent("terminalUpdate");
    }
    _doFeed(data) {
      const affectedLines = this.vt.feed(data);
      affectedLines.forEach(i => this.changedLines.add(i));
      this.cursor = undefined;
    }
    _now() {
      return performance.now() * this.speed;
    }
    async _initializeDriver() {
      const meta = await this.driver.init();
      this.cols = this.cols ?? meta.cols ?? 80;
      this.rows = this.rows ?? meta.rows ?? 24;
      this.duration = this.duration ?? meta.duration;
      this.markers = this._normalizeMarkers(meta.markers) ?? this.markers ?? [];
      if (this.cols === 0) {
        this.cols = 80;
      }
      if (this.rows === 0) {
        this.rows = 24;
      }
      this._initializeVt(this.cols, this.rows);
      const poster = meta.poster !== undefined ? this._renderPoster(meta.poster) : null;
      this._dispatchEvent("metadata", {
        cols: this.cols,
        rows: this.rows,
        duration: this.duration,
        markers: this.markers,
        theme: meta.theme,
        poster
      });
    }
    _resetVt(cols, rows) {
      let init = arguments.length > 2 && arguments[2] !== undefined ? arguments[2] : undefined;
      let theme = arguments.length > 3 && arguments[3] !== undefined ? arguments[3] : undefined;
      this.logger.debug(`core: vt reset (${cols}x${rows})`);
      this.cols = cols;
      this.rows = rows;
      this.cursor = undefined;
      this._initializeVt(cols, rows);
      if (init !== undefined && init !== "") {
        this._doFeed(init);
      }
      this._dispatchEvent("reset", {
        cols,
        rows,
        theme
      });
    }
    _resizeVt(cols, rows) {
      if (cols === this.vt.cols && rows === this.vt.rows) return;
      const affectedLines = this.vt.resize(cols, rows);
      affectedLines.forEach(i => this.changedLines.add(i));
      this.cursor = undefined;
      this.vt.cols = cols;
      this.vt.rows = rows;
      this.logger.debug(`core: vt resize (${cols}x${rows})`);
      this._dispatchEvent("resize", {
        cols,
        rows
      });
    }
    _initializeVt(cols, rows) {
      this.vt = this.wasm.create(cols, rows, true, 100);
      this.vt.cols = cols;
      this.vt.rows = rows;
      this.changedLines.clear();
      for (let i = 0; i < rows; i++) {
        this.changedLines.add(i);
      }
    }
    _parsePoster(poster) {
      if (typeof poster !== "string") return {};
      if (poster.substring(0, 16) == "data:text/plain,") {
        return {
          type: "text",
          value: [poster.substring(16)]
        };
      } else if (poster.substring(0, 4) == "npt:") {
        return {
          type: "npt",
          value: parseNpt(poster.substring(4))
        };
      }
      return {};
    }
    _renderPoster(poster) {
      const cols = this.cols ?? 80;
      const rows = this.rows ?? 24;
      this.logger.debug(`core: poster init (${cols}x${rows})`);
      const vt = this.wasm.create(cols, rows, false, 0);
      poster.forEach(text => vt.feed(text));
      const cursor = vt.getCursor() ?? false;
      const lines = [];
      for (let i = 0; i < rows; i++) {
        lines.push({
          id: i,
          segments: vt.getLine(i)
        });
      }
      return {
        cursor,
        lines
      };
    }
    _normalizeMarkers(markers) {
      if (Array.isArray(markers)) {
        return markers.map(m => typeof m === "number" ? [m, ""] : m);
      }
    }
  }
  const DRIVERS = new Map([["benchmark", benchmark], ["clock", clock], ["eventsource", eventsource], ["random", random], ["recording", recording], ["websocket", websocket]]);
  const PARSERS = new Map([["asciicast", parse$2], ["typescript", parse$1], ["ttyrec", parse]]);
  function getDriver(src) {
    if (typeof src === "function") return src;
    if (typeof src === "string") {
      if (src.substring(0, 5) == "ws://" || src.substring(0, 6) == "wss://") {
        src = {
          driver: "websocket",
          url: src
        };
      } else if (src.substring(0, 6) == "clock:") {
        src = {
          driver: "clock"
        };
      } else if (src.substring(0, 7) == "random:") {
        src = {
          driver: "random"
        };
      } else if (src.substring(0, 10) == "benchmark:") {
        src = {
          driver: "benchmark",
          url: src.substring(10)
        };
      } else {
        src = {
          driver: "recording",
          url: src
        };
      }
    }
    if (src.driver === undefined) {
      src.driver = "recording";
    }
    if (src.driver == "recording") {
      if (src.parser === undefined) {
        src.parser = "asciicast";
      }
      if (typeof src.parser === "string") {
        if (PARSERS.has(src.parser)) {
          src.parser = PARSERS.get(src.parser);
        } else {
          throw `unknown parser: ${src.parser}`;
        }
      }
    }
    if (DRIVERS.has(src.driver)) {
      const driver = DRIVERS.get(src.driver);
      return (callbacks, opts) => driver(src, callbacks, opts);
    } else {
      throw `unsupported driver: ${JSON.stringify(src)}`;
    }
  }

  const IS_DEV = false;
  const equalFn = (a, b) => a === b;
  const $PROXY = Symbol("solid-proxy");
  const SUPPORTS_PROXY = typeof Proxy === "function";
  const $TRACK = Symbol("solid-track");
  const signalOptions = {
    equals: equalFn
  };
  let runEffects = runQueue;
  const STALE = 1;
  const PENDING = 2;
  const UNOWNED = {
    owned: null,
    cleanups: null,
    context: null,
    owner: null
  };
  var Owner = null;
  let Transition = null;
  let ExternalSourceConfig = null;
  let Listener = null;
  let Updates = null;
  let Effects = null;
  let ExecCount = 0;
  function createRoot(fn, detachedOwner) {
    const listener = Listener,
      owner = Owner,
      unowned = fn.length === 0,
      current = detachedOwner === undefined ? owner : detachedOwner,
      root = unowned
        ? UNOWNED
        : {
            owned: null,
            cleanups: null,
            context: current ? current.context : null,
            owner: current
          },
      updateFn = unowned ? fn : () => fn(() => untrack(() => cleanNode(root)));
    Owner = root;
    Listener = null;
    try {
      return runUpdates(updateFn, true);
    } finally {
      Listener = listener;
      Owner = owner;
    }
  }
  function createSignal(value, options) {
    options = options ? Object.assign({}, signalOptions, options) : signalOptions;
    const s = {
      value,
      observers: null,
      observerSlots: null,
      comparator: options.equals || undefined
    };
    const setter = value => {
      if (typeof value === "function") {
        value = value(s.value);
      }
      return writeSignal(s, value);
    };
    return [readSignal.bind(s), setter];
  }
  function createRenderEffect(fn, value, options) {
    const c = createComputation(fn, value, false, STALE);
    updateComputation(c);
  }
  function createEffect(fn, value, options) {
    runEffects = runUserEffects;
    const c = createComputation(fn, value, false, STALE);
    if (!options || !options.render) c.user = true;
    Effects ? Effects.push(c) : updateComputation(c);
  }
  function createMemo(fn, value, options) {
    options = options ? Object.assign({}, signalOptions, options) : signalOptions;
    const c = createComputation(fn, value, true, 0);
    c.observers = null;
    c.observerSlots = null;
    c.comparator = options.equals || undefined;
    updateComputation(c);
    return readSignal.bind(c);
  }
  function batch(fn) {
    return runUpdates(fn, false);
  }
  function untrack(fn) {
    if (Listener === null) return fn();
    const listener = Listener;
    Listener = null;
    try {
      if (ExternalSourceConfig) ;
      return fn();
    } finally {
      Listener = listener;
    }
  }
  function onMount(fn) {
    createEffect(() => untrack(fn));
  }
  function onCleanup(fn) {
    if (Owner === null);
    else if (Owner.cleanups === null) Owner.cleanups = [fn];
    else Owner.cleanups.push(fn);
    return fn;
  }
  function getListener() {
    return Listener;
  }
  function children(fn) {
    const children = createMemo(fn);
    const memo = createMemo(() => resolveChildren(children()));
    memo.toArray = () => {
      const c = memo();
      return Array.isArray(c) ? c : c != null ? [c] : [];
    };
    return memo;
  }
  function readSignal() {
    if (this.sources && (this.state)) {
      if ((this.state) === STALE) updateComputation(this);
      else {
        const updates = Updates;
        Updates = null;
        runUpdates(() => lookUpstream(this), false);
        Updates = updates;
      }
    }
    if (Listener) {
      const sSlot = this.observers ? this.observers.length : 0;
      if (!Listener.sources) {
        Listener.sources = [this];
        Listener.sourceSlots = [sSlot];
      } else {
        Listener.sources.push(this);
        Listener.sourceSlots.push(sSlot);
      }
      if (!this.observers) {
        this.observers = [Listener];
        this.observerSlots = [Listener.sources.length - 1];
      } else {
        this.observers.push(Listener);
        this.observerSlots.push(Listener.sources.length - 1);
      }
    }
    return this.value;
  }
  function writeSignal(node, value, isComp) {
    let current =
      node.value;
    if (!node.comparator || !node.comparator(current, value)) {
      node.value = value;
      if (node.observers && node.observers.length) {
        runUpdates(() => {
          for (let i = 0; i < node.observers.length; i += 1) {
            const o = node.observers[i];
            const TransitionRunning = Transition && Transition.running;
            if (TransitionRunning && Transition.disposed.has(o)) ;
            if (TransitionRunning ? !o.tState : !o.state) {
              if (o.pure) Updates.push(o);
              else Effects.push(o);
              if (o.observers) markDownstream(o);
            }
            if (!TransitionRunning) o.state = STALE;
          }
          if (Updates.length > 10e5) {
            Updates = [];
            if (IS_DEV);
            throw new Error();
          }
        }, false);
      }
    }
    return value;
  }
  function updateComputation(node) {
    if (!node.fn) return;
    cleanNode(node);
    const time = ExecCount;
    runComputation(
      node,
      node.value,
      time
    );
  }
  function runComputation(node, value, time) {
    let nextValue;
    const owner = Owner,
      listener = Listener;
    Listener = Owner = node;
    try {
      nextValue = node.fn(value);
    } catch (err) {
      if (node.pure) {
        {
          node.state = STALE;
          node.owned && node.owned.forEach(cleanNode);
          node.owned = null;
        }
      }
      node.updatedAt = time + 1;
      return handleError(err);
    } finally {
      Listener = listener;
      Owner = owner;
    }
    if (!node.updatedAt || node.updatedAt <= time) {
      if (node.updatedAt != null && "observers" in node) {
        writeSignal(node, nextValue);
      } else node.value = nextValue;
      node.updatedAt = time;
    }
  }
  function createComputation(fn, init, pure, state = STALE, options) {
    const c = {
      fn,
      state: state,
      updatedAt: null,
      owned: null,
      sources: null,
      sourceSlots: null,
      cleanups: null,
      value: init,
      owner: Owner,
      context: Owner ? Owner.context : null,
      pure
    };
    if (Owner === null);
    else if (Owner !== UNOWNED) {
      {
        if (!Owner.owned) Owner.owned = [c];
        else Owner.owned.push(c);
      }
    }
    return c;
  }
  function runTop(node) {
    if ((node.state) === 0) return;
    if ((node.state) === PENDING) return lookUpstream(node);
    if (node.suspense && untrack(node.suspense.inFallback)) return node.suspense.effects.push(node);
    const ancestors = [node];
    while ((node = node.owner) && (!node.updatedAt || node.updatedAt < ExecCount)) {
      if (node.state) ancestors.push(node);
    }
    for (let i = ancestors.length - 1; i >= 0; i--) {
      node = ancestors[i];
      if ((node.state) === STALE) {
        updateComputation(node);
      } else if ((node.state) === PENDING) {
        const updates = Updates;
        Updates = null;
        runUpdates(() => lookUpstream(node, ancestors[0]), false);
        Updates = updates;
      }
    }
  }
  function runUpdates(fn, init) {
    if (Updates) return fn();
    let wait = false;
    if (!init) Updates = [];
    if (Effects) wait = true;
    else Effects = [];
    ExecCount++;
    try {
      const res = fn();
      completeUpdates(wait);
      return res;
    } catch (err) {
      if (!wait) Effects = null;
      Updates = null;
      handleError(err);
    }
  }
  function completeUpdates(wait) {
    if (Updates) {
      runQueue(Updates);
      Updates = null;
    }
    if (wait) return;
    const e = Effects;
    Effects = null;
    if (e.length) runUpdates(() => runEffects(e), false);
  }
  function runQueue(queue) {
    for (let i = 0; i < queue.length; i++) runTop(queue[i]);
  }
  function runUserEffects(queue) {
    let i,
      userLength = 0;
    for (i = 0; i < queue.length; i++) {
      const e = queue[i];
      if (!e.user) runTop(e);
      else queue[userLength++] = e;
    }
    for (i = 0; i < userLength; i++) runTop(queue[i]);
  }
  function lookUpstream(node, ignore) {
    node.state = 0;
    for (let i = 0; i < node.sources.length; i += 1) {
      const source = node.sources[i];
      if (source.sources) {
        const state = source.state;
        if (state === STALE) {
          if (source !== ignore && (!source.updatedAt || source.updatedAt < ExecCount))
            runTop(source);
        } else if (state === PENDING) lookUpstream(source, ignore);
      }
    }
  }
  function markDownstream(node) {
    for (let i = 0; i < node.observers.length; i += 1) {
      const o = node.observers[i];
      if (!o.state) {
        o.state = PENDING;
        if (o.pure) Updates.push(o);
        else Effects.push(o);
        o.observers && markDownstream(o);
      }
    }
  }
  function cleanNode(node) {
    let i;
    if (node.sources) {
      while (node.sources.length) {
        const source = node.sources.pop(),
          index = node.sourceSlots.pop(),
          obs = source.observers;
        if (obs && obs.length) {
          const n = obs.pop(),
            s = source.observerSlots.pop();
          if (index < obs.length) {
            n.sourceSlots[s] = index;
            obs[index] = n;
            source.observerSlots[index] = s;
          }
        }
      }
    }
    if (node.tOwned) {
      for (i = node.tOwned.length - 1; i >= 0; i--) cleanNode(node.tOwned[i]);
      delete node.tOwned;
    }
    if (node.owned) {
      for (i = node.owned.length - 1; i >= 0; i--) cleanNode(node.owned[i]);
      node.owned = null;
    }
    if (node.cleanups) {
      for (i = node.cleanups.length - 1; i >= 0; i--) node.cleanups[i]();
      node.cleanups = null;
    }
    node.state = 0;
  }
  function castError(err) {
    if (err instanceof Error) return err;
    return new Error(typeof err === "string" ? err : "Unknown error", {
      cause: err
    });
  }
  function handleError(err, owner = Owner) {
    const error = castError(err);
    throw error;
  }
  function resolveChildren(children) {
    if (typeof children === "function" && !children.length) return resolveChildren(children());
    if (Array.isArray(children)) {
      const results = [];
      for (let i = 0; i < children.length; i++) {
        const result = resolveChildren(children[i]);
        Array.isArray(result) ? results.push.apply(results, result) : results.push(result);
      }
      return results;
    }
    return children;
  }

  const FALLBACK = Symbol("fallback");
  function dispose(d) {
    for (let i = 0; i < d.length; i++) d[i]();
  }
  function mapArray(list, mapFn, options = {}) {
    let items = [],
      mapped = [],
      disposers = [],
      len = 0,
      indexes = mapFn.length > 1 ? [] : null;
    onCleanup(() => dispose(disposers));
    return () => {
      let newItems = list() || [],
        newLen = newItems.length,
        i,
        j;
      newItems[$TRACK];
      return untrack(() => {
        let newIndices, newIndicesNext, temp, tempdisposers, tempIndexes, start, end, newEnd, item;
        if (newLen === 0) {
          if (len !== 0) {
            dispose(disposers);
            disposers = [];
            items = [];
            mapped = [];
            len = 0;
            indexes && (indexes = []);
          }
          if (options.fallback) {
            items = [FALLBACK];
            mapped[0] = createRoot(disposer => {
              disposers[0] = disposer;
              return options.fallback();
            });
            len = 1;
          }
        } else if (len === 0) {
          mapped = new Array(newLen);
          for (j = 0; j < newLen; j++) {
            items[j] = newItems[j];
            mapped[j] = createRoot(mapper);
          }
          len = newLen;
        } else {
          temp = new Array(newLen);
          tempdisposers = new Array(newLen);
          indexes && (tempIndexes = new Array(newLen));
          for (
            start = 0, end = Math.min(len, newLen);
            start < end && items[start] === newItems[start];
            start++
          );
          for (
            end = len - 1, newEnd = newLen - 1;
            end >= start && newEnd >= start && items[end] === newItems[newEnd];
            end--, newEnd--
          ) {
            temp[newEnd] = mapped[end];
            tempdisposers[newEnd] = disposers[end];
            indexes && (tempIndexes[newEnd] = indexes[end]);
          }
          newIndices = new Map();
          newIndicesNext = new Array(newEnd + 1);
          for (j = newEnd; j >= start; j--) {
            item = newItems[j];
            i = newIndices.get(item);
            newIndicesNext[j] = i === undefined ? -1 : i;
            newIndices.set(item, j);
          }
          for (i = start; i <= end; i++) {
            item = items[i];
            j = newIndices.get(item);
            if (j !== undefined && j !== -1) {
              temp[j] = mapped[i];
              tempdisposers[j] = disposers[i];
              indexes && (tempIndexes[j] = indexes[i]);
              j = newIndicesNext[j];
              newIndices.set(item, j);
            } else disposers[i]();
          }
          for (j = start; j < newLen; j++) {
            if (j in temp) {
              mapped[j] = temp[j];
              disposers[j] = tempdisposers[j];
              if (indexes) {
                indexes[j] = tempIndexes[j];
                indexes[j](j);
              }
            } else mapped[j] = createRoot(mapper);
          }
          mapped = mapped.slice(0, (len = newLen));
          items = newItems.slice(0);
        }
        return mapped;
      });
      function mapper(disposer) {
        disposers[j] = disposer;
        if (indexes) {
          const [s, set] = createSignal(j);
          indexes[j] = set;
          return mapFn(newItems[j], s);
        }
        return mapFn(newItems[j]);
      }
    };
  }
  function indexArray(list, mapFn, options = {}) {
    let items = [],
      mapped = [],
      disposers = [],
      signals = [],
      len = 0,
      i;
    onCleanup(() => dispose(disposers));
    return () => {
      const newItems = list() || [],
        newLen = newItems.length;
      newItems[$TRACK];
      return untrack(() => {
        if (newLen === 0) {
          if (len !== 0) {
            dispose(disposers);
            disposers = [];
            items = [];
            mapped = [];
            len = 0;
            signals = [];
          }
          if (options.fallback) {
            items = [FALLBACK];
            mapped[0] = createRoot(disposer => {
              disposers[0] = disposer;
              return options.fallback();
            });
            len = 1;
          }
          return mapped;
        }
        if (items[0] === FALLBACK) {
          disposers[0]();
          disposers = [];
          items = [];
          mapped = [];
          len = 0;
        }
        for (i = 0; i < newLen; i++) {
          if (i < items.length && items[i] !== newItems[i]) {
            signals[i](() => newItems[i]);
          } else if (i >= items.length) {
            mapped[i] = createRoot(mapper);
          }
        }
        for (; i < items.length; i++) {
          disposers[i]();
        }
        len = signals.length = disposers.length = newLen;
        items = newItems.slice(0);
        return (mapped = mapped.slice(0, len));
      });
      function mapper(disposer) {
        disposers[i] = disposer;
        const [s, set] = createSignal(newItems[i]);
        signals[i] = set;
        return mapFn(s, i);
      }
    };
  }
  function createComponent(Comp, props) {
    return untrack(() => Comp(props || {}));
  }
  function trueFn() {
    return true;
  }
  const propTraps = {
    get(_, property, receiver) {
      if (property === $PROXY) return receiver;
      return _.get(property);
    },
    has(_, property) {
      if (property === $PROXY) return true;
      return _.has(property);
    },
    set: trueFn,
    deleteProperty: trueFn,
    getOwnPropertyDescriptor(_, property) {
      return {
        configurable: true,
        enumerable: true,
        get() {
          return _.get(property);
        },
        set: trueFn,
        deleteProperty: trueFn
      };
    },
    ownKeys(_) {
      return _.keys();
    }
  };
  function resolveSource(s) {
    return !(s = typeof s === "function" ? s() : s) ? {} : s;
  }
  function resolveSources() {
    for (let i = 0, length = this.length; i < length; ++i) {
      const v = this[i]();
      if (v !== undefined) return v;
    }
  }
  function mergeProps(...sources) {
    let proxy = false;
    for (let i = 0; i < sources.length; i++) {
      const s = sources[i];
      proxy = proxy || (!!s && $PROXY in s);
      sources[i] = typeof s === "function" ? ((proxy = true), createMemo(s)) : s;
    }
    if (SUPPORTS_PROXY && proxy) {
      return new Proxy(
        {
          get(property) {
            for (let i = sources.length - 1; i >= 0; i--) {
              const v = resolveSource(sources[i])[property];
              if (v !== undefined) return v;
            }
          },
          has(property) {
            for (let i = sources.length - 1; i >= 0; i--) {
              if (property in resolveSource(sources[i])) return true;
            }
            return false;
          },
          keys() {
            const keys = [];
            for (let i = 0; i < sources.length; i++)
              keys.push(...Object.keys(resolveSource(sources[i])));
            return [...new Set(keys)];
          }
        },
        propTraps
      );
    }
    const sourcesMap = {};
    const defined = Object.create(null);
    for (let i = sources.length - 1; i >= 0; i--) {
      const source = sources[i];
      if (!source) continue;
      const sourceKeys = Object.getOwnPropertyNames(source);
      for (let i = sourceKeys.length - 1; i >= 0; i--) {
        const key = sourceKeys[i];
        if (key === "__proto__" || key === "constructor") continue;
        const desc = Object.getOwnPropertyDescriptor(source, key);
        if (!defined[key]) {
          defined[key] = desc.get
            ? {
                enumerable: true,
                configurable: true,
                get: resolveSources.bind((sourcesMap[key] = [desc.get.bind(source)]))
              }
            : desc.value !== undefined
            ? desc
            : undefined;
        } else {
          const sources = sourcesMap[key];
          if (sources) {
            if (desc.get) sources.push(desc.get.bind(source));
            else if (desc.value !== undefined) sources.push(() => desc.value);
          }
        }
      }
    }
    const target = {};
    const definedKeys = Object.keys(defined);
    for (let i = definedKeys.length - 1; i >= 0; i--) {
      const key = definedKeys[i],
        desc = defined[key];
      if (desc && desc.get) Object.defineProperty(target, key, desc);
      else target[key] = desc ? desc.value : undefined;
    }
    return target;
  }

  const narrowedError = name => `Stale read from <${name}>.`;
  function For(props) {
    const fallback = "fallback" in props && {
      fallback: () => props.fallback
    };
    return createMemo(mapArray(() => props.each, props.children, fallback || undefined));
  }
  function Index(props) {
    const fallback = "fallback" in props && {
      fallback: () => props.fallback
    };
    return createMemo(indexArray(() => props.each, props.children, fallback || undefined));
  }
  function Show(props) {
    const keyed = props.keyed;
    const conditionValue = createMemo(() => props.when, undefined, undefined);
    const condition = keyed
      ? conditionValue
      : createMemo(conditionValue, undefined, {
          equals: (a, b) => !a === !b
        });
    return createMemo(
      () => {
        const c = condition();
        if (c) {
          const child = props.children;
          const fn = typeof child === "function" && child.length > 0;
          return fn
            ? untrack(() =>
                child(
                  keyed
                    ? c
                    : () => {
                        if (!untrack(condition)) throw narrowedError("Show");
                        return conditionValue();
                      }
                )
              )
            : child;
        }
        return props.fallback;
      },
      undefined,
      undefined
    );
  }
  function Switch(props) {
    const chs = children(() => props.children);
    const switchFunc = createMemo(() => {
      const ch = chs();
      const mps = Array.isArray(ch) ? ch : [ch];
      let func = () => undefined;
      for (let i = 0; i < mps.length; i++) {
        const index = i;
        const mp = mps[i];
        const prevFunc = func;
        const conditionValue = createMemo(
          () => (prevFunc() ? undefined : mp.when),
          undefined,
          undefined
        );
        const condition = mp.keyed
          ? conditionValue
          : createMemo(conditionValue, undefined, {
              equals: (a, b) => !a === !b
            });
        func = () => prevFunc() || (condition() ? [index, conditionValue, mp] : undefined);
      }
      return func;
    });
    return createMemo(
      () => {
        const sel = switchFunc()();
        if (!sel) return props.fallback;
        const [index, conditionValue, mp] = sel;
        const child = mp.children;
        const fn = typeof child === "function" && child.length > 0;
        return fn
          ? untrack(() =>
              child(
                mp.keyed
                  ? conditionValue()
                  : () => {
                      if (untrack(switchFunc)()?.[0] !== index) throw narrowedError("Match");
                      return conditionValue();
                    }
              )
            )
          : child;
      },
      undefined,
      undefined
    );
  }
  function Match(props) {
    return props;
  }

  const memo = fn => createMemo(() => fn());

  function reconcileArrays(parentNode, a, b) {
    let bLength = b.length,
      aEnd = a.length,
      bEnd = bLength,
      aStart = 0,
      bStart = 0,
      after = a[aEnd - 1].nextSibling,
      map = null;
    while (aStart < aEnd || bStart < bEnd) {
      if (a[aStart] === b[bStart]) {
        aStart++;
        bStart++;
        continue;
      }
      while (a[aEnd - 1] === b[bEnd - 1]) {
        aEnd--;
        bEnd--;
      }
      if (aEnd === aStart) {
        const node = bEnd < bLength ? (bStart ? b[bStart - 1].nextSibling : b[bEnd - bStart]) : after;
        while (bStart < bEnd) parentNode.insertBefore(b[bStart++], node);
      } else if (bEnd === bStart) {
        while (aStart < aEnd) {
          if (!map || !map.has(a[aStart])) a[aStart].remove();
          aStart++;
        }
      } else if (a[aStart] === b[bEnd - 1] && b[bStart] === a[aEnd - 1]) {
        const node = a[--aEnd].nextSibling;
        parentNode.insertBefore(b[bStart++], a[aStart++].nextSibling);
        parentNode.insertBefore(b[--bEnd], node);
        a[aEnd] = b[bEnd];
      } else {
        if (!map) {
          map = new Map();
          let i = bStart;
          while (i < bEnd) map.set(b[i], i++);
        }
        const index = map.get(a[aStart]);
        if (index != null) {
          if (bStart < index && index < bEnd) {
            let i = aStart,
              sequence = 1,
              t;
            while (++i < aEnd && i < bEnd) {
              if ((t = map.get(a[i])) == null || t !== index + sequence) break;
              sequence++;
            }
            if (sequence > index - bStart) {
              const node = a[aStart];
              while (bStart < index) parentNode.insertBefore(b[bStart++], node);
            } else parentNode.replaceChild(b[bStart++], a[aStart++]);
          } else aStart++;
        } else a[aStart++].remove();
      }
    }
  }

  const $$EVENTS = "_$DX_DELEGATE";
  function render(code, element, init, options = {}) {
    let disposer;
    createRoot(dispose => {
      disposer = dispose;
      element === document
        ? code()
        : insert(element, code(), element.firstChild ? null : undefined, init);
    }, options.owner);
    return () => {
      disposer();
      element.textContent = "";
    };
  }
  function template(html, isImportNode, isSVG, isMathML) {
    let node;
    const create = () => {
      const t = isMathML
        ? document.createElementNS("http://www.w3.org/1998/Math/MathML", "template")
        : document.createElement("template");
      t.innerHTML = html;
      return isSVG ? t.content.firstChild.firstChild : isMathML ? t.firstChild : t.content.firstChild;
    };
    const fn = isImportNode
      ? () => untrack(() => document.importNode(node || (node = create()), true))
      : () => (node || (node = create())).cloneNode(true);
    fn.cloneNode = fn;
    return fn;
  }
  function delegateEvents(eventNames, document = window.document) {
    const e = document[$$EVENTS] || (document[$$EVENTS] = new Set());
    for (let i = 0, l = eventNames.length; i < l; i++) {
      const name = eventNames[i];
      if (!e.has(name)) {
        e.add(name);
        document.addEventListener(name, eventHandler);
      }
    }
  }
  function setAttribute(node, name, value) {
    if (value == null) node.removeAttribute(name);
    else node.setAttribute(name, value);
  }
  function className(node, value) {
    if (value == null) node.removeAttribute("class");
    else node.className = value;
  }
  function addEventListener(node, name, handler, delegate) {
    if (delegate) {
      if (Array.isArray(handler)) {
        node[`$$${name}`] = handler[0];
        node[`$$${name}Data`] = handler[1];
      } else node[`$$${name}`] = handler;
    } else if (Array.isArray(handler)) {
      const handlerFn = handler[0];
      node.addEventListener(name, (handler[0] = e => handlerFn.call(node, handler[1], e)));
    } else node.addEventListener(name, handler, typeof handler !== "function" && handler);
  }
  function style(node, value, prev) {
    if (!value) return prev ? setAttribute(node, "style") : value;
    const nodeStyle = node.style;
    if (typeof value === "string") return (nodeStyle.cssText = value);
    typeof prev === "string" && (nodeStyle.cssText = prev = undefined);
    prev || (prev = {});
    value || (value = {});
    let v, s;
    for (s in prev) {
      value[s] == null && nodeStyle.removeProperty(s);
      delete prev[s];
    }
    for (s in value) {
      v = value[s];
      if (v !== prev[s]) {
        nodeStyle.setProperty(s, v);
        prev[s] = v;
      }
    }
    return prev;
  }
  function use(fn, element, arg) {
    return untrack(() => fn(element, arg));
  }
  function insert(parent, accessor, marker, initial) {
    if (marker !== undefined && !initial) initial = [];
    if (typeof accessor !== "function") return insertExpression(parent, accessor, initial, marker);
    createRenderEffect(current => insertExpression(parent, accessor(), current, marker), initial);
  }
  function eventHandler(e) {
    let node = e.target;
    const key = `$$${e.type}`;
    const oriTarget = e.target;
    const oriCurrentTarget = e.currentTarget;
    const retarget = value =>
      Object.defineProperty(e, "target", {
        configurable: true,
        value
      });
    const handleNode = () => {
      const handler = node[key];
      if (handler && !node.disabled) {
        const data = node[`${key}Data`];
        data !== undefined ? handler.call(node, data, e) : handler.call(node, e);
        if (e.cancelBubble) return;
      }
      node.host &&
        typeof node.host !== "string" &&
        !node.host._$host &&
        node.contains(e.target) &&
        retarget(node.host);
      return true;
    };
    const walkUpTree = () => {
      while (handleNode() && (node = node._$host || node.parentNode || node.host));
    };
    Object.defineProperty(e, "currentTarget", {
      configurable: true,
      get() {
        return node || document;
      }
    });
    if (e.composedPath) {
      const path = e.composedPath();
      retarget(path[0]);
      for (let i = 0; i < path.length - 2; i++) {
        node = path[i];
        if (!handleNode()) break;
        if (node._$host) {
          node = node._$host;
          walkUpTree();
          break;
        }
        if (node.parentNode === oriCurrentTarget) {
          break;
        }
      }
    } else walkUpTree();
    retarget(oriTarget);
  }
  function insertExpression(parent, value, current, marker, unwrapArray) {
    while (typeof current === "function") current = current();
    if (value === current) return current;
    const t = typeof value,
      multi = marker !== undefined;
    parent = (multi && current[0] && current[0].parentNode) || parent;
    if (t === "string" || t === "number") {
      if (t === "number") {
        value = value.toString();
        if (value === current) return current;
      }
      if (multi) {
        let node = current[0];
        if (node && node.nodeType === 3) {
          node.data !== value && (node.data = value);
        } else node = document.createTextNode(value);
        current = cleanChildren(parent, current, marker, node);
      } else {
        if (current !== "" && typeof current === "string") {
          current = parent.firstChild.data = value;
        } else current = parent.textContent = value;
      }
    } else if (value == null || t === "boolean") {
      current = cleanChildren(parent, current, marker);
    } else if (t === "function") {
      createRenderEffect(() => {
        let v = value();
        while (typeof v === "function") v = v();
        current = insertExpression(parent, v, current, marker);
      });
      return () => current;
    } else if (Array.isArray(value)) {
      const array = [];
      const currentArray = current && Array.isArray(current);
      if (normalizeIncomingArray(array, value, current, unwrapArray)) {
        createRenderEffect(() => (current = insertExpression(parent, array, current, marker, true)));
        return () => current;
      }
      if (array.length === 0) {
        current = cleanChildren(parent, current, marker);
        if (multi) return current;
      } else if (currentArray) {
        if (current.length === 0) {
          appendNodes(parent, array, marker);
        } else reconcileArrays(parent, current, array);
      } else {
        current && cleanChildren(parent);
        appendNodes(parent, array);
      }
      current = array;
    } else if (value.nodeType) {
      if (Array.isArray(current)) {
        if (multi) return (current = cleanChildren(parent, current, marker, value));
        cleanChildren(parent, current, null, value);
      } else if (current == null || current === "" || !parent.firstChild) {
        parent.appendChild(value);
      } else parent.replaceChild(value, parent.firstChild);
      current = value;
    } else;
    return current;
  }
  function normalizeIncomingArray(normalized, array, current, unwrap) {
    let dynamic = false;
    for (let i = 0, len = array.length; i < len; i++) {
      let item = array[i],
        prev = current && current[normalized.length],
        t;
      if (item == null || item === true || item === false);
      else if ((t = typeof item) === "object" && item.nodeType) {
        normalized.push(item);
      } else if (Array.isArray(item)) {
        dynamic = normalizeIncomingArray(normalized, item, prev) || dynamic;
      } else if (t === "function") {
        if (unwrap) {
          while (typeof item === "function") item = item();
          dynamic =
            normalizeIncomingArray(
              normalized,
              Array.isArray(item) ? item : [item],
              Array.isArray(prev) ? prev : [prev]
            ) || dynamic;
        } else {
          normalized.push(item);
          dynamic = true;
        }
      } else {
        const value = String(item);
        if (prev && prev.nodeType === 3 && prev.data === value) normalized.push(prev);
        else normalized.push(document.createTextNode(value));
      }
    }
    return dynamic;
  }
  function appendNodes(parent, array, marker = null) {
    for (let i = 0, len = array.length; i < len; i++) parent.insertBefore(array[i], marker);
  }
  function cleanChildren(parent, current, marker, replacement) {
    if (marker === undefined) return (parent.textContent = "");
    const node = replacement || document.createTextNode("");
    if (current.length) {
      let inserted = false;
      for (let i = current.length - 1; i >= 0; i--) {
        const el = current[i];
        if (node !== el) {
          const isParent = el.parentNode === parent;
          if (!inserted && !i)
            isParent ? parent.replaceChild(node, el) : parent.insertBefore(node, marker);
          else isParent && el.remove();
        } else inserted = true;
      }
    } else parent.insertBefore(node, marker);
    return [node];
  }

  const $RAW = Symbol("store-raw"),
    $NODE = Symbol("store-node"),
    $HAS = Symbol("store-has"),
    $SELF = Symbol("store-self");
  function wrap$1(value) {
    let p = value[$PROXY];
    if (!p) {
      Object.defineProperty(value, $PROXY, {
        value: (p = new Proxy(value, proxyTraps$1))
      });
      if (!Array.isArray(value)) {
        const keys = Object.keys(value),
          desc = Object.getOwnPropertyDescriptors(value);
        for (let i = 0, l = keys.length; i < l; i++) {
          const prop = keys[i];
          if (desc[prop].get) {
            Object.defineProperty(value, prop, {
              enumerable: desc[prop].enumerable,
              get: desc[prop].get.bind(p)
            });
          }
        }
      }
    }
    return p;
  }
  function isWrappable(obj) {
    let proto;
    return (
      obj != null &&
      typeof obj === "object" &&
      (obj[$PROXY] ||
        !(proto = Object.getPrototypeOf(obj)) ||
        proto === Object.prototype ||
        Array.isArray(obj))
    );
  }
  function unwrap(item, set = new Set()) {
    let result, unwrapped, v, prop;
    if ((result = item != null && item[$RAW])) return result;
    if (!isWrappable(item) || set.has(item)) return item;
    if (Array.isArray(item)) {
      if (Object.isFrozen(item)) item = item.slice(0);
      else set.add(item);
      for (let i = 0, l = item.length; i < l; i++) {
        v = item[i];
        if ((unwrapped = unwrap(v, set)) !== v) item[i] = unwrapped;
      }
    } else {
      if (Object.isFrozen(item)) item = Object.assign({}, item);
      else set.add(item);
      const keys = Object.keys(item),
        desc = Object.getOwnPropertyDescriptors(item);
      for (let i = 0, l = keys.length; i < l; i++) {
        prop = keys[i];
        if (desc[prop].get) continue;
        v = item[prop];
        if ((unwrapped = unwrap(v, set)) !== v) item[prop] = unwrapped;
      }
    }
    return item;
  }
  function getNodes(target, symbol) {
    let nodes = target[symbol];
    if (!nodes)
      Object.defineProperty(target, symbol, {
        value: (nodes = Object.create(null))
      });
    return nodes;
  }
  function getNode(nodes, property, value) {
    if (nodes[property]) return nodes[property];
    const [s, set] = createSignal(value, {
      equals: false,
      internal: true
    });
    s.$ = set;
    return (nodes[property] = s);
  }
  function proxyDescriptor$1(target, property) {
    const desc = Reflect.getOwnPropertyDescriptor(target, property);
    if (!desc || desc.get || !desc.configurable || property === $PROXY || property === $NODE)
      return desc;
    delete desc.value;
    delete desc.writable;
    desc.get = () => target[$PROXY][property];
    return desc;
  }
  function trackSelf(target) {
    getListener() && getNode(getNodes(target, $NODE), $SELF)();
  }
  function ownKeys(target) {
    trackSelf(target);
    return Reflect.ownKeys(target);
  }
  const proxyTraps$1 = {
    get(target, property, receiver) {
      if (property === $RAW) return target;
      if (property === $PROXY) return receiver;
      if (property === $TRACK) {
        trackSelf(target);
        return receiver;
      }
      const nodes = getNodes(target, $NODE);
      const tracked = nodes[property];
      let value = tracked ? tracked() : target[property];
      if (property === $NODE || property === $HAS || property === "__proto__") return value;
      if (!tracked) {
        const desc = Object.getOwnPropertyDescriptor(target, property);
        if (
          getListener() &&
          (typeof value !== "function" || target.hasOwnProperty(property)) &&
          !(desc && desc.get)
        )
          value = getNode(nodes, property, value)();
      }
      return isWrappable(value) ? wrap$1(value) : value;
    },
    has(target, property) {
      if (
        property === $RAW ||
        property === $PROXY ||
        property === $TRACK ||
        property === $NODE ||
        property === $HAS ||
        property === "__proto__"
      )
        return true;
      getListener() && getNode(getNodes(target, $HAS), property)();
      return property in target;
    },
    set() {
      return true;
    },
    deleteProperty() {
      return true;
    },
    ownKeys: ownKeys,
    getOwnPropertyDescriptor: proxyDescriptor$1
  };
  function setProperty(state, property, value, deleting = false) {
    if (!deleting && state[property] === value) return;
    const prev = state[property],
      len = state.length;
    if (value === undefined) {
      delete state[property];
      if (state[$HAS] && state[$HAS][property] && prev !== undefined) state[$HAS][property].$();
    } else {
      state[property] = value;
      if (state[$HAS] && state[$HAS][property] && prev === undefined) state[$HAS][property].$();
    }
    let nodes = getNodes(state, $NODE),
      node;
    if ((node = getNode(nodes, property, prev))) node.$(() => value);
    if (Array.isArray(state) && state.length !== len) {
      for (let i = state.length; i < len; i++) (node = nodes[i]) && node.$();
      (node = getNode(nodes, "length", len)) && node.$(state.length);
    }
    (node = nodes[$SELF]) && node.$();
  }
  function mergeStoreNode(state, value) {
    const keys = Object.keys(value);
    for (let i = 0; i < keys.length; i += 1) {
      const key = keys[i];
      setProperty(state, key, value[key]);
    }
  }
  function updateArray(current, next) {
    if (typeof next === "function") next = next(current);
    next = unwrap(next);
    if (Array.isArray(next)) {
      if (current === next) return;
      let i = 0,
        len = next.length;
      for (; i < len; i++) {
        const value = next[i];
        if (current[i] !== value) setProperty(current, i, value);
      }
      setProperty(current, "length", len);
    } else mergeStoreNode(current, next);
  }
  function updatePath(current, path, traversed = []) {
    let part,
      prev = current;
    if (path.length > 1) {
      part = path.shift();
      const partType = typeof part,
        isArray = Array.isArray(current);
      if (Array.isArray(part)) {
        for (let i = 0; i < part.length; i++) {
          updatePath(current, [part[i]].concat(path), traversed);
        }
        return;
      } else if (isArray && partType === "function") {
        for (let i = 0; i < current.length; i++) {
          if (part(current[i], i)) updatePath(current, [i].concat(path), traversed);
        }
        return;
      } else if (isArray && partType === "object") {
        const { from = 0, to = current.length - 1, by = 1 } = part;
        for (let i = from; i <= to; i += by) {
          updatePath(current, [i].concat(path), traversed);
        }
        return;
      } else if (path.length > 1) {
        updatePath(current[part], path, [part].concat(traversed));
        return;
      }
      prev = current[part];
      traversed = [part].concat(traversed);
    }
    let value = path[0];
    if (typeof value === "function") {
      value = value(prev, traversed);
      if (value === prev) return;
    }
    if (part === undefined && value == undefined) return;
    value = unwrap(value);
    if (part === undefined || (isWrappable(prev) && isWrappable(value) && !Array.isArray(value))) {
      mergeStoreNode(prev, value);
    } else setProperty(current, part, value);
  }
  function createStore(...[store, options]) {
    const unwrappedStore = unwrap(store || {});
    const isArray = Array.isArray(unwrappedStore);
    const wrappedStore = wrap$1(unwrappedStore);
    function setStore(...args) {
      batch(() => {
        isArray && args.length === 1
          ? updateArray(unwrappedStore, args[0])
          : updatePath(unwrappedStore, args);
      });
    }
    return [wrappedStore, setStore];
  }

  const $ROOT = Symbol("store-root");
  function applyState(target, parent, property, merge, key) {
    const previous = parent[property];
    if (target === previous) return;
    const isArray = Array.isArray(target);
    if (
      property !== $ROOT &&
      (!isWrappable(target) ||
        !isWrappable(previous) ||
        isArray !== Array.isArray(previous) ||
        (key && target[key] !== previous[key]))
    ) {
      setProperty(parent, property, target);
      return;
    }
    if (isArray) {
      if (
        target.length &&
        previous.length &&
        (!merge || (key && target[0] && target[0][key] != null))
      ) {
        let i, j, start, end, newEnd, item, newIndicesNext, keyVal;
        for (
          start = 0, end = Math.min(previous.length, target.length);
          start < end &&
          (previous[start] === target[start] ||
            (key && previous[start] && target[start] && previous[start][key] === target[start][key]));
          start++
        ) {
          applyState(target[start], previous, start, merge, key);
        }
        const temp = new Array(target.length),
          newIndices = new Map();
        for (
          end = previous.length - 1, newEnd = target.length - 1;
          end >= start &&
          newEnd >= start &&
          (previous[end] === target[newEnd] ||
            (key && previous[end] && target[newEnd] && previous[end][key] === target[newEnd][key]));
          end--, newEnd--
        ) {
          temp[newEnd] = previous[end];
        }
        if (start > newEnd || start > end) {
          for (j = start; j <= newEnd; j++) setProperty(previous, j, target[j]);
          for (; j < target.length; j++) {
            setProperty(previous, j, temp[j]);
            applyState(target[j], previous, j, merge, key);
          }
          if (previous.length > target.length) setProperty(previous, "length", target.length);
          return;
        }
        newIndicesNext = new Array(newEnd + 1);
        for (j = newEnd; j >= start; j--) {
          item = target[j];
          keyVal = key && item ? item[key] : item;
          i = newIndices.get(keyVal);
          newIndicesNext[j] = i === undefined ? -1 : i;
          newIndices.set(keyVal, j);
        }
        for (i = start; i <= end; i++) {
          item = previous[i];
          keyVal = key && item ? item[key] : item;
          j = newIndices.get(keyVal);
          if (j !== undefined && j !== -1) {
            temp[j] = previous[i];
            j = newIndicesNext[j];
            newIndices.set(keyVal, j);
          }
        }
        for (j = start; j < target.length; j++) {
          if (j in temp) {
            setProperty(previous, j, temp[j]);
            applyState(target[j], previous, j, merge, key);
          } else setProperty(previous, j, target[j]);
        }
      } else {
        for (let i = 0, len = target.length; i < len; i++) {
          applyState(target[i], previous, i, merge, key);
        }
      }
      if (previous.length > target.length) setProperty(previous, "length", target.length);
      return;
    }
    const targetKeys = Object.keys(target);
    for (let i = 0, len = targetKeys.length; i < len; i++) {
      applyState(target[targetKeys[i]], previous, targetKeys[i], merge, key);
    }
    const previousKeys = Object.keys(previous);
    for (let i = 0, len = previousKeys.length; i < len; i++) {
      if (target[previousKeys[i]] === undefined) setProperty(previous, previousKeys[i], undefined);
    }
  }
  function reconcile(value, options = {}) {
    const { merge, key = "id" } = options,
      v = unwrap(value);
    return state => {
      if (!isWrappable(state) || !isWrappable(v)) return v;
      const res = applyState(
        v,
        {
          [$ROOT]: state
        },
        $ROOT,
        merge,
        key
      );
      return res === undefined ? state : res;
    };
  }

  const _tmpl$$9 = /*#__PURE__*/template(`<span></span>`, 2);
  var Segment = (props => {
    const codePoint = createMemo(() => {
      if (props.text.length == 1) {
        const cp = props.text.codePointAt(0);
        if (cp >= 0x2580 && cp <= 0x259f || cp == 0xe0b0 || cp == 0xe0b2) {
          return cp;
        }
      }
    });
    const text = createMemo(() => codePoint() ? " " : props.text);
    const style$1 = createMemo(() => buildStyle(props.pen, props.offset, props.cellCount));
    const className$1 = createMemo(() => buildClassName(props.pen, codePoint(), props.extraClass));
    return (() => {
      const _el$ = _tmpl$$9.cloneNode(true);
      insert(_el$, text);
      createRenderEffect(_p$ => {
        const _v$ = className$1(),
          _v$2 = style$1();
        _v$ !== _p$._v$ && className(_el$, _p$._v$ = _v$);
        _p$._v$2 = style(_el$, _v$2, _p$._v$2);
        return _p$;
      }, {
        _v$: undefined,
        _v$2: undefined
      });
      return _el$;
    })();
  });
  function buildClassName(attrs, codePoint, extraClass) {
    const fgClass = colorClass(attrs.get("fg"), attrs.get("bold"), "fg-");
    const bgClass = colorClass(attrs.get("bg"), false, "bg-");
    let cls = extraClass ?? "";
    if (codePoint !== undefined) {
      cls += ` cp-${codePoint.toString(16)}`;
    }
    if (fgClass) {
      cls += " " + fgClass;
    }
    if (bgClass) {
      cls += " " + bgClass;
    }
    if (attrs.has("bold")) {
      cls += " ap-bright";
    }
    if (attrs.has("faint")) {
      cls += " ap-faint";
    }
    if (attrs.has("italic")) {
      cls += " ap-italic";
    }
    if (attrs.has("underline")) {
      cls += " ap-underline";
    }
    if (attrs.has("blink")) {
      cls += " ap-blink";
    }
    if (attrs.get("inverse")) {
      cls += " ap-inverse";
    }
    return cls;
  }
  function colorClass(color, intense, prefix) {
    if (typeof color === "number") {
      if (intense && color < 8) {
        color += 8;
      }
      return `${prefix}${color}`;
    }
  }
  function buildStyle(attrs, offset, width) {
    const fg = attrs.get("fg");
    const bg = attrs.get("bg");
    let style = {
      "--offset": offset,
      width: `${width + 0.01}ch`
    };
    if (typeof fg === "string") {
      style["--fg"] = fg;
    }
    if (typeof bg === "string") {
      style["--bg"] = bg;
    }
    return style;
  }

  const _tmpl$$8 = /*#__PURE__*/template(`<span class="ap-line" role="paragraph"></span>`, 2);
  var Line = (props => {
    const segments = () => {
      if (typeof props.cursor === "number") {
        const segs = [];
        let cellOffset = 0;
        let segIndex = 0;
        while (segIndex < props.segments.length && cellOffset + props.segments[segIndex].cellCount - 1 < props.cursor) {
          const seg = props.segments[segIndex];
          segs.push(seg);
          cellOffset += seg.cellCount;
          segIndex++;
        }
        if (segIndex < props.segments.length) {
          const seg = props.segments[segIndex];
          const charWidth = seg.charWidth;
          let cellIndex = props.cursor - cellOffset;
          const charIndex = Math.floor(cellIndex / charWidth);
          cellIndex = charIndex * charWidth;
          const chars = Array.from(seg.text);
          if (charIndex > 0) {
            segs.push({
              ...seg,
              text: chars.slice(0, charIndex).join("")
            });
          }
          segs.push({
            ...seg,
            text: chars[charIndex],
            offset: cellOffset + cellIndex,
            cellCount: charWidth,
            extraClass: "ap-cursor"
          });
          if (charIndex < chars.length - 1) {
            segs.push({
              ...seg,
              text: chars.slice(charIndex + 1).join(""),
              offset: cellOffset + cellIndex + 1,
              cellCount: seg.cellCount - charWidth
            });
          }
          segIndex++;
          while (segIndex < props.segments.length) {
            const seg = props.segments[segIndex];
            segs.push(seg);
            segIndex++;
          }
        }
        return segs;
      } else {
        return props.segments;
      }
    };
    return (() => {
      const _el$ = _tmpl$$8.cloneNode(true);
      insert(_el$, createComponent(Index, {
        get each() {
          return segments();
        },
        children: s => createComponent(Segment, mergeProps(s))
      }));
      return _el$;
    })();
  });

  const _tmpl$$7 = /*#__PURE__*/template(`<pre class="ap-terminal" aria-live="off" tabindex="0"></pre>`, 2);
  var Terminal = (props => {
    const lineHeight = () => props.lineHeight ?? 1.3333333333;
    const style$1 = createMemo(() => {
      return {
        width: `${props.cols}ch`,
        height: `${lineHeight() * props.rows}em`,
        "font-size": `${(props.scale || 1.0) * 100}%`,
        "font-family": props.fontFamily,
        "--term-line-height": `${lineHeight()}em`,
        "--term-cols": props.cols
      };
    });
    const cursorCol = createMemo(() => props.cursor?.[0]);
    const cursorRow = createMemo(() => props.cursor?.[1]);
    return (() => {
      const _el$ = _tmpl$$7.cloneNode(true);
      const _ref$ = props.ref;
      typeof _ref$ === "function" ? use(_ref$, _el$) : props.ref = _el$;
      insert(_el$, createComponent(For, {
        get each() {
          return props.lines;
        },
        children: (line, i) => createComponent(Line, {
          get segments() {
            return line.segments;
          },
          get cursor() {
            return memo(() => i() === cursorRow())() ? cursorCol() : null;
          }
        })
      }));
      createRenderEffect(_p$ => {
        const _v$ = !!(props.blink || props.cursorHold),
          _v$2 = !!props.blink,
          _v$3 = style$1();
        _v$ !== _p$._v$ && _el$.classList.toggle("ap-cursor-on", _p$._v$ = _v$);
        _v$2 !== _p$._v$2 && _el$.classList.toggle("ap-blink", _p$._v$2 = _v$2);
        _p$._v$3 = style(_el$, _v$3, _p$._v$3);
        return _p$;
      }, {
        _v$: undefined,
        _v$2: undefined,
        _v$3: undefined
      });
      return _el$;
    })();
  });

  const _tmpl$$6 = /*#__PURE__*/template(`<svg version="1.1" viewBox="0 0 12 12" class="ap-icon" aria-label="Pause" role="button"><path d="M1,0 L4,0 L4,12 L1,12 Z"></path><path d="M8,0 L11,0 L11,12 L8,12 Z"></path></svg>`, 6),
    _tmpl$2$1 = /*#__PURE__*/template(`<svg version="1.1" viewBox="0 0 12 12" class="ap-icon" aria-label="Play" role="button"><path d="M1,0 L11,6 L1,12 Z"></path></svg>`, 4),
    _tmpl$3$1 = /*#__PURE__*/template(`<span class="ap-button ap-playback-button" tabindex="0"></span>`, 2),
    _tmpl$4$1 = /*#__PURE__*/template(`<span class="ap-bar"><span class="ap-gutter ap-gutter-empty"></span><span class="ap-gutter ap-gutter-full"></span></span>`, 6),
    _tmpl$5$1 = /*#__PURE__*/template(`<div class="ap-control-bar"><span class="ap-timer" aria-readonly="true" role="textbox" tabindex="0"><span class="ap-time-elapsed"></span><span class="ap-time-remaining"></span></span><span class="ap-progressbar"></span><span class="ap-button ap-kbd-button ap-tooltip-container" aria-label="Show keyboard shortcuts" role="button" tabindex="0"><svg version="1.1" viewBox="6 8 14 16" class="ap-icon"><path d="M0.938 8.313h22.125c0.5 0 0.938 0.438 0.938 0.938v13.5c0 0.5-0.438 0.938-0.938 0.938h-22.125c-0.5 0-0.938-0.438-0.938-0.938v-13.5c0-0.5 0.438-0.938 0.938-0.938zM1.594 22.063h20.813v-12.156h-20.813v12.156zM3.844 11.188h1.906v1.938h-1.906v-1.938zM7.469 11.188h1.906v1.938h-1.906v-1.938zM11.031 11.188h1.938v1.938h-1.938v-1.938zM14.656 11.188h1.875v1.938h-1.875v-1.938zM18.25 11.188h1.906v1.938h-1.906v-1.938zM5.656 15.031h1.938v1.938h-1.938v-1.938zM9.281 16.969v-1.938h1.906v1.938h-1.906zM12.875 16.969v-1.938h1.906v1.938h-1.906zM18.406 16.969h-1.938v-1.938h1.938v1.938zM16.531 20.781h-9.063v-1.906h9.063v1.906z"></path></svg><span class="ap-tooltip">Keyboard shortcuts (?)</span></span><span class="ap-button ap-fullscreen-button ap-tooltip-container" aria-label="Toggle fullscreen mode" role="button" tabindex="0"><svg version="1.1" viewBox="0 0 12 12" class="ap-icon ap-icon-fullscreen-on"><path d="M12,0 L7,0 L9,2 L7,4 L8,5 L10,3 L12,5 Z"></path><path d="M0,12 L0,7 L2,9 L4,7 L5,8 L3,10 L5,12 Z"></path></svg><svg version="1.1" viewBox="0 0 12 12" class="ap-icon ap-icon-fullscreen-off"><path d="M7,5 L7,0 L9,2 L11,0 L12,1 L10,3 L12,5 Z"></path><path d="M5,7 L0,7 L2,9 L0,11 L1,12 L3,10 L5,12 Z"></path></svg><span class="ap-tooltip">Fullscreen (f)</span></span></div>`, 34),
    _tmpl$6$1 = /*#__PURE__*/template(`<span class="ap-marker-container ap-tooltip-container"><span class="ap-marker"></span><span class="ap-tooltip"></span></span>`, 6);
  function formatTime(seconds) {
    let s = Math.floor(seconds);
    const d = Math.floor(s / 86400);
    s %= 86400;
    const h = Math.floor(s / 3600);
    s %= 3600;
    const m = Math.floor(s / 60);
    s %= 60;
    if (d > 0) {
      return `${zeroPad(d)}:${zeroPad(h)}:${zeroPad(m)}:${zeroPad(s)}`;
    } else if (h > 0) {
      return `${zeroPad(h)}:${zeroPad(m)}:${zeroPad(s)}`;
    } else {
      return `${zeroPad(m)}:${zeroPad(s)}`;
    }
  }
  function zeroPad(n) {
    return n < 10 ? `0${n}` : n.toString();
  }
  var ControlBar = (props => {
    const e = f => {
      return e => {
        e.preventDefault();
        f(e);
      };
    };
    const currentTime = () => typeof props.currentTime === "number" ? formatTime(props.currentTime) : "--:--";
    const remainingTime = () => typeof props.remainingTime === "number" ? "-" + formatTime(props.remainingTime) : currentTime();
    const markers = createMemo(() => typeof props.duration === "number" ? props.markers.filter(m => m[0] < props.duration) : []);
    const markerPosition = m => `${m[0] / props.duration * 100}%`;
    const markerText = m => {
      if (m[1] === "") {
        return formatTime(m[0]);
      } else {
        return `${formatTime(m[0])} - ${m[1]}`;
      }
    };
    const isPastMarker = m => typeof props.currentTime === "number" ? m[0] <= props.currentTime : false;
    const gutterBarStyle = () => {
      return {
        transform: `scaleX(${props.progress || 0}`
      };
    };
    const calcPosition = e => {
      const barWidth = e.currentTarget.offsetWidth;
      const rect = e.currentTarget.getBoundingClientRect();
      const mouseX = e.clientX - rect.left;
      const pos = Math.max(0, mouseX / barWidth);
      return `${pos * 100}%`;
    };
    const [mouseDown, setMouseDown] = createSignal(false);
    const throttledSeek = throttle(props.onSeekClick, 50);
    const onMouseDown = e => {
      if (e._marker) return;
      if (e.altKey || e.shiftKey || e.metaKey || e.ctrlKey || e.button !== 0) return;
      setMouseDown(true);
      props.onSeekClick(calcPosition(e));
    };
    const seekToMarker = index => {
      return e(() => {
        props.onSeekClick({
          marker: index
        });
      });
    };
    const onMove = e => {
      if (e.altKey || e.shiftKey || e.metaKey || e.ctrlKey) return;
      if (mouseDown()) {
        throttledSeek(calcPosition(e));
      }
    };
    const onDocumentMouseUp = () => {
      setMouseDown(false);
    };
    document.addEventListener("mouseup", onDocumentMouseUp);
    onCleanup(() => {
      document.removeEventListener("mouseup", onDocumentMouseUp);
    });
    return (() => {
      const _el$ = _tmpl$5$1.cloneNode(true),
        _el$5 = _el$.firstChild,
        _el$6 = _el$5.firstChild,
        _el$7 = _el$6.nextSibling,
        _el$8 = _el$5.nextSibling,
        _el$12 = _el$8.nextSibling,
        _el$13 = _el$12.nextSibling;
      const _ref$ = props.ref;
      typeof _ref$ === "function" ? use(_ref$, _el$) : props.ref = _el$;
      insert(_el$, createComponent(Show, {
        get when() {
          return props.isPausable;
        },
        get children() {
          const _el$2 = _tmpl$3$1.cloneNode(true);
          addEventListener(_el$2, "click", e(props.onPlayClick), true);
          insert(_el$2, createComponent(Switch, {
            get children() {
              return [createComponent(Match, {
                get when() {
                  return props.isPlaying;
                },
                get children() {
                  return _tmpl$$6.cloneNode(true);
                }
              }), createComponent(Match, {
                get when() {
                  return !props.isPlaying;
                },
                get children() {
                  return _tmpl$2$1.cloneNode(true);
                }
              })];
            }
          }));
          return _el$2;
        }
      }), _el$5);
      insert(_el$6, currentTime);
      insert(_el$7, remainingTime);
      insert(_el$8, createComponent(Show, {
        get when() {
          return typeof props.progress === "number" || props.isSeekable;
        },
        get children() {
          const _el$9 = _tmpl$4$1.cloneNode(true),
            _el$10 = _el$9.firstChild,
            _el$11 = _el$10.nextSibling;
          _el$9.$$mousemove = onMove;
          _el$9.$$mousedown = onMouseDown;
          insert(_el$9, createComponent(For, {
            get each() {
              return markers();
            },
            children: (m, i) => (() => {
              const _el$14 = _tmpl$6$1.cloneNode(true),
                _el$15 = _el$14.firstChild,
                _el$16 = _el$15.nextSibling;
              _el$14.$$mousedown = e => {
                e._marker = true;
              };
              addEventListener(_el$14, "click", seekToMarker(i()), true);
              insert(_el$16, () => markerText(m));
              createRenderEffect(_p$ => {
                const _v$ = markerPosition(m),
                  _v$2 = !!isPastMarker(m);
                _v$ !== _p$._v$ && _el$14.style.setProperty("left", _p$._v$ = _v$);
                _v$2 !== _p$._v$2 && _el$15.classList.toggle("ap-marker-past", _p$._v$2 = _v$2);
                return _p$;
              }, {
                _v$: undefined,
                _v$2: undefined
              });
              return _el$14;
            })()
          }), null);
          createRenderEffect(_$p => style(_el$11, gutterBarStyle(), _$p));
          return _el$9;
        }
      }));
      addEventListener(_el$12, "click", e(props.onHelpClick), true);
      addEventListener(_el$13, "click", e(props.onFullscreenClick), true);
      createRenderEffect(() => _el$.classList.toggle("ap-seekable", !!props.isSeekable));
      return _el$;
    })();
  });
  delegateEvents(["click", "mousedown", "mousemove"]);

  const _tmpl$$5 = /*#__PURE__*/template(`<div class="ap-overlay ap-overlay-error"><span>💥</span></div>`, 4);
  var ErrorOverlay = (props => {
    return _tmpl$$5.cloneNode(true);
  });

  const _tmpl$$4 = /*#__PURE__*/template(`<div class="ap-overlay ap-overlay-loading"><span class="ap-loader"></span></div>`, 4);
  var LoaderOverlay = (props => {
    return _tmpl$$4.cloneNode(true);
  });

  const _tmpl$$3 = /*#__PURE__*/template(`<div class="ap-overlay ap-overlay-info"><span></span></div>`, 4);
  var InfoOverlay = (props => {
    const style$1 = () => {
      return {
        "font-family": props.fontFamily
      };
    };
    return (() => {
      const _el$ = _tmpl$$3.cloneNode(true),
        _el$2 = _el$.firstChild;
      insert(_el$2, () => props.message);
      createRenderEffect(_$p => style(_el$2, style$1(), _$p));
      return _el$;
    })();
  });

  const _tmpl$$2 = /*#__PURE__*/template(`<div class="ap-overlay ap-overlay-start"><div class="ap-play-button"><div><span><svg version="1.1" viewBox="0 0 1000.0 1000.0" class="ap-icon"><defs><mask id="small-triangle-mask"><rect width="100%" height="100%" fill="white"></rect><polygon points="700.0 500.0, 400.00000000000006 326.7949192431122, 399.9999999999999 673.2050807568877" fill="black"></polygon></mask></defs><polygon points="1000.0 500.0, 250.0000000000001 66.98729810778059, 249.99999999999977 933.0127018922192" mask="url(#small-triangle-mask)" fill="white" class="ap-play-btn-fill"></polygon><polyline points="673.2050807568878 400.0, 326.7949192431123 600.0" stroke="white" stroke-width="90" class="ap-play-btn-stroke"></polyline></svg></span></div></div></div>`, 22);
  var StartOverlay = (props => {
    const e = f => {
      return e => {
        e.preventDefault();
        f(e);
      };
    };
    return (() => {
      const _el$ = _tmpl$$2.cloneNode(true);
      addEventListener(_el$, "click", e(props.onClick), true);
      return _el$;
    })();
  });
  delegateEvents(["click"]);

  const _tmpl$$1 = /*#__PURE__*/template(`<li><kbd>space</kbd> - pause / resume</li>`, 4),
    _tmpl$2 = /*#__PURE__*/template(`<li><kbd>←</kbd> / <kbd>→</kbd> - rewind / fast-forward by 5 seconds</li>`, 6),
    _tmpl$3 = /*#__PURE__*/template(`<li><kbd>Shift</kbd> + <kbd>←</kbd> / <kbd>→</kbd> - rewind / fast-forward by 10%</li>`, 8),
    _tmpl$4 = /*#__PURE__*/template(`<li><kbd>[</kbd> / <kbd>]</kbd> - jump to the previous / next marker</li>`, 6),
    _tmpl$5 = /*#__PURE__*/template(`<li><kbd>0</kbd>, <kbd>1</kbd>, <kbd>2</kbd> ... <kbd>9</kbd> - jump to 0%, 10%, 20% ... 90%</li>`, 10),
    _tmpl$6 = /*#__PURE__*/template(`<li><kbd>,</kbd> / <kbd>.</kbd> - step back / forward, a frame at a time (when paused)</li>`, 6),
    _tmpl$7 = /*#__PURE__*/template(`<div class="ap-overlay ap-overlay-help"><div><div><p>Keyboard shortcuts</p><ul><li><kbd>f</kbd> - toggle fullscreen mode</li><li><kbd>?</kbd> - toggle this help popup</li></ul></div></div></div>`, 18);
  var HelpOverlay = (props => {
    const style$1 = () => {
      return {
        "font-family": props.fontFamily
      };
    };
    const e = f => {
      return e => {
        e.preventDefault();
        f(e);
      };
    };
    return (() => {
      const _el$ = _tmpl$7.cloneNode(true),
        _el$2 = _el$.firstChild,
        _el$3 = _el$2.firstChild,
        _el$4 = _el$3.firstChild,
        _el$5 = _el$4.nextSibling,
        _el$12 = _el$5.firstChild;
      addEventListener(_el$, "click", e(props.onClose), true);
      _el$2.$$click = e => {
        e.stopPropagation();
      };
      insert(_el$5, createComponent(Show, {
        get when() {
          return props.isPausable;
        },
        get children() {
          return _tmpl$$1.cloneNode(true);
        }
      }), _el$12);
      insert(_el$5, createComponent(Show, {
        get when() {
          return props.isSeekable;
        },
        get children() {
          return [_tmpl$2.cloneNode(true), _tmpl$3.cloneNode(true), _tmpl$4.cloneNode(true), _tmpl$5.cloneNode(true), _tmpl$6.cloneNode(true)];
        }
      }), _el$12);
      createRenderEffect(_$p => style(_el$, style$1(), _$p));
      return _el$;
    })();
  });
  delegateEvents(["click"]);

  const _tmpl$ = /*#__PURE__*/template(`<div class="ap-wrapper" tabindex="-1"><div></div></div>`, 4);
  const CONTROL_BAR_HEIGHT = 32; // must match height of div.ap-control-bar in CSS

  var Player = (props => {
    const logger = props.logger;
    const core = props.core;
    const autoPlay = props.autoPlay;
    const [state, setState] = createStore({
      lines: [],
      cursor: undefined,
      charW: props.charW,
      charH: props.charH,
      bordersW: props.bordersW,
      bordersH: props.bordersH,
      containerW: 0,
      containerH: 0,
      isPausable: true,
      isSeekable: true,
      isFullscreen: false,
      currentTime: null,
      remainingTime: null,
      progress: null,
      blink: true,
      cursorHold: false
    });
    const [isPlaying, setIsPlaying] = createSignal(false);
    const [overlay, setOverlay] = createSignal(!autoPlay ? "start" : null);
    const [infoMessage, setInfoMessage] = createSignal(null);
    const [terminalSize, setTerminalSize] = createSignal({
      cols: props.cols,
      rows: props.rows
    }, {
      equals: (newVal, oldVal) => newVal.cols === oldVal.cols && newVal.rows === oldVal.rows
    });
    const [duration, setDuration] = createSignal(undefined);
    const [markers, setMarkers] = createStore([]);
    const [userActive, setUserActive] = createSignal(false);
    const [isHelpVisible, setIsHelpVisible] = createSignal(false);
    const [originalTheme, setOriginalTheme] = createSignal(undefined);
    const terminalCols = createMemo(() => terminalSize().cols || 80);
    const terminalRows = createMemo(() => terminalSize().rows || 24);
    const controlBarHeight = () => props.controls === false ? 0 : CONTROL_BAR_HEIGHT;
    const controlsVisible = () => props.controls === true || props.controls === "auto" && userActive();
    let frameRequestId;
    let userActivityTimeoutId;
    let timeUpdateIntervalId;
    let blinkIntervalId;
    let wrapperRef;
    let playerRef;
    let terminalRef;
    let controlBarRef;
    let resizeObserver;
    function onPlaying() {
      updateTerminal();
      startBlinking();
      startTimeUpdates();
    }
    function onStopped() {
      stopBlinking();
      stopTimeUpdates();
      updateTime();
    }
    function resize(size_) {
      batch(() => {
        if (size_.rows < terminalSize().rows) {
          setState("lines", state.lines.slice(0, size_.rows));
        }
        setTerminalSize(size_);
      });
    }
    function setPoster(poster) {
      if (poster !== null && !autoPlay) {
        setState({
          lines: poster.lines,
          cursor: poster.cursor
        });
      }
    }
    let resolveCoreReady;
    const coreReady = new Promise(resolve => {
      resolveCoreReady = resolve;
    });
    core.addEventListener("ready", _ref => {
      let {
        isPausable,
        isSeekable,
        poster
      } = _ref;
      setState({
        isPausable,
        isSeekable
      });
      setPoster(poster);
      resolveCoreReady();
    });
    core.addEventListener("metadata", _ref2 => {
      let {
        cols,
        rows,
        duration,
        theme,
        poster,
        markers
      } = _ref2;
      batch(() => {
        resize({
          cols,
          rows
        });
        setDuration(duration);
        setOriginalTheme(theme);
        setMarkers(markers);
        setPoster(poster);
      });
    });
    core.addEventListener("play", () => {
      setOverlay(null);
    });
    core.addEventListener("playing", () => {
      batch(() => {
        setIsPlaying(true);
        setOverlay(null);
        onPlaying();
      });
    });
    core.addEventListener("idle", () => {
      batch(() => {
        setIsPlaying(false);
        onStopped();
      });
    });
    core.addEventListener("loading", () => {
      batch(() => {
        setIsPlaying(false);
        onStopped();
        setOverlay("loader");
      });
    });
    core.addEventListener("offline", _ref3 => {
      let {
        message
      } = _ref3;
      batch(() => {
        setIsPlaying(false);
        onStopped();
        if (message !== undefined) {
          setInfoMessage(message);
          setOverlay("info");
        }
      });
    });
    let renderCount = 0;
    core.addEventListener("ended", _ref4 => {
      let {
        message
      } = _ref4;
      batch(() => {
        setIsPlaying(false);
        onStopped();
        if (message !== undefined) {
          setInfoMessage(message);
          setOverlay("info");
        }
      });
      logger.debug(`view: render count: ${renderCount}`);
    });
    core.addEventListener("errored", () => {
      setOverlay("error");
    });
    core.addEventListener("resize", resize);
    core.addEventListener("reset", _ref5 => {
      let {
        cols,
        rows,
        theme
      } = _ref5;
      batch(() => {
        resize({
          cols,
          rows
        });
        setOriginalTheme(theme);
        updateTerminal();
      });
    });
    core.addEventListener("seeked", () => {
      updateTime();
    });
    core.addEventListener("terminalUpdate", () => {
      if (frameRequestId === undefined) {
        frameRequestId = requestAnimationFrame(updateTerminal);
      }
    });
    const setupResizeObserver = () => {
      resizeObserver = new ResizeObserver(debounce(_entries => {
        setState({
          containerW: wrapperRef.offsetWidth,
          containerH: wrapperRef.offsetHeight
        });
        wrapperRef.dispatchEvent(new CustomEvent("resize", {
          detail: {
            el: playerRef
          }
        }));
      }, 10));
      resizeObserver.observe(wrapperRef);
    };
    onMount(async () => {
      logger.info("view: mounted");
      logger.debug("view: font measurements", {
        charW: state.charW,
        charH: state.charH
      });
      setupResizeObserver();
      setState({
        containerW: wrapperRef.offsetWidth,
        containerH: wrapperRef.offsetHeight
      });
    });
    onCleanup(() => {
      core.stop();
      stopBlinking();
      stopTimeUpdates();
      resizeObserver.disconnect();
    });
    const updateTerminal = async () => {
      const changes = await core.getChanges();
      batch(() => {
        if (changes.lines !== undefined) {
          changes.lines.forEach((line, i) => {
            setState("lines", i, reconcile(line));
          });
        }
        if (changes.cursor !== undefined) {
          setState("cursor", reconcile(changes.cursor));
        }
        setState("cursorHold", true);
      });
      frameRequestId = undefined;
      renderCount += 1;
    };
    const terminalElementSize = createMemo(() => {
      const terminalW = state.charW * terminalCols() + state.bordersW;
      const terminalH = state.charH * terminalRows() + state.bordersH;
      let fit = props.fit ?? "width";
      if (fit === "both" || state.isFullscreen) {
        const containerRatio = state.containerW / (state.containerH - controlBarHeight());
        const terminalRatio = terminalW / terminalH;
        if (containerRatio > terminalRatio) {
          fit = "height";
        } else {
          fit = "width";
        }
      }
      if (fit === false || fit === "none") {
        return {};
      } else if (fit === "width") {
        const scale = state.containerW / terminalW;
        return {
          scale: scale,
          width: state.containerW,
          height: terminalH * scale + controlBarHeight()
        };
      } else if (fit === "height") {
        const scale = (state.containerH - controlBarHeight()) / terminalH;
        return {
          scale: scale,
          width: terminalW * scale,
          height: state.containerH
        };
      } else {
        throw `unsupported fit mode: ${fit}`;
      }
    });
    const onFullscreenChange = () => {
      setState("isFullscreen", document.fullscreenElement ?? document.webkitFullscreenElement);
    };
    const toggleFullscreen = () => {
      if (state.isFullscreen) {
        (document.exitFullscreen ?? document.webkitExitFullscreen ?? (() => {})).apply(document);
      } else {
        (wrapperRef.requestFullscreen ?? wrapperRef.webkitRequestFullscreen ?? (() => {})).apply(wrapperRef);
      }
    };
    const toggleHelp = () => {
      if (isHelpVisible()) {
        setIsHelpVisible(false);
      } else {
        core.pause();
        setIsHelpVisible(true);
      }
    };
    const onKeyDown = e => {
      if (e.altKey || e.metaKey || e.ctrlKey) {
        return;
      }
      if (e.key == " ") {
        core.togglePlay();
      } else if (e.key == ",") {
        core.step(-1);
        updateTime();
      } else if (e.key == ".") {
        core.step();
        updateTime();
      } else if (e.key == "f") {
        toggleFullscreen();
      } else if (e.key == "[") {
        core.seek({
          marker: "prev"
        });
      } else if (e.key == "]") {
        core.seek({
          marker: "next"
        });
      } else if (e.key.charCodeAt(0) >= 48 && e.key.charCodeAt(0) <= 57) {
        const pos = (e.key.charCodeAt(0) - 48) / 10;
        core.seek(`${pos * 100}%`);
      } else if (e.key == "?") {
        toggleHelp();
      } else if (e.key == "ArrowLeft") {
        if (e.shiftKey) {
          core.seek("<<<");
        } else {
          core.seek("<<");
        }
      } else if (e.key == "ArrowRight") {
        if (e.shiftKey) {
          core.seek(">>>");
        } else {
          core.seek(">>");
        }
      } else if (e.key == "Escape") {
        setIsHelpVisible(false);
      } else {
        return;
      }
      e.stopPropagation();
      e.preventDefault();
    };
    const wrapperOnMouseMove = () => {
      if (state.isFullscreen) {
        onUserActive(true);
      }
    };
    const playerOnMouseLeave = () => {
      if (!state.isFullscreen) {
        onUserActive(false);
      }
    };
    const startTimeUpdates = () => {
      timeUpdateIntervalId = setInterval(updateTime, 100);
    };
    const stopTimeUpdates = () => {
      clearInterval(timeUpdateIntervalId);
    };
    const updateTime = async () => {
      const currentTime = await core.getCurrentTime();
      const remainingTime = await core.getRemainingTime();
      const progress = await core.getProgress();
      setState({
        currentTime,
        remainingTime,
        progress
      });
    };
    const startBlinking = () => {
      blinkIntervalId = setInterval(() => {
        setState(state => {
          const changes = {
            blink: !state.blink
          };
          if (changes.blink) {
            changes.cursorHold = false;
          }
          return changes;
        });
      }, 600);
    };
    const stopBlinking = () => {
      clearInterval(blinkIntervalId);
      setState("blink", true);
    };
    const onUserActive = show => {
      clearTimeout(userActivityTimeoutId);
      if (show) {
        userActivityTimeoutId = setTimeout(() => onUserActive(false), 2000);
      }
      setUserActive(show);
    };
    const theme = createMemo(() => {
      const name = props.theme || "auto/asciinema";
      if (name.slice(0, 5) === "auto/") {
        return {
          name: name.slice(5),
          colors: originalTheme()
        };
      } else {
        return {
          name
        };
      }
    });
    const playerStyle = () => {
      const style = {};
      if ((props.fit === false || props.fit === "none") && props.terminalFontSize !== undefined) {
        if (props.terminalFontSize === "small") {
          style["font-size"] = "12px";
        } else if (props.terminalFontSize === "medium") {
          style["font-size"] = "18px";
        } else if (props.terminalFontSize === "big") {
          style["font-size"] = "24px";
        } else {
          style["font-size"] = props.terminalFontSize;
        }
      }
      const size = terminalElementSize();
      if (size.width !== undefined) {
        style["width"] = `${size.width}px`;
        style["height"] = `${size.height}px`;
      }
      const themeColors = theme().colors;
      if (themeColors) {
        style["--term-color-foreground"] = themeColors.foreground;
        style["--term-color-background"] = themeColors.background;
        themeColors.palette.forEach((color, i) => {
          style[`--term-color-${i}`] = color;
        });
      }
      return style;
    };
    const play = () => {
      coreReady.then(() => core.play());
    };
    const togglePlay = () => {
      coreReady.then(() => core.togglePlay());
    };
    const seek = pos => {
      coreReady.then(() => core.seek(pos));
    };
    const playerClass = () => `ap-player asciinema-player-theme-${theme().name}`;
    const terminalScale = () => terminalElementSize()?.scale;
    const el = (() => {
      const _el$ = _tmpl$.cloneNode(true),
        _el$2 = _el$.firstChild;
      const _ref$ = wrapperRef;
      typeof _ref$ === "function" ? use(_ref$, _el$) : wrapperRef = _el$;
      _el$.addEventListener("webkitfullscreenchange", onFullscreenChange);
      _el$.addEventListener("fullscreenchange", onFullscreenChange);
      _el$.$$mousemove = wrapperOnMouseMove;
      _el$.$$keydown = onKeyDown;
      const _ref$2 = playerRef;
      typeof _ref$2 === "function" ? use(_ref$2, _el$2) : playerRef = _el$2;
      _el$2.$$mousemove = () => onUserActive(true);
      _el$2.addEventListener("mouseleave", playerOnMouseLeave);
      insert(_el$2, createComponent(Terminal, {
        get cols() {
          return terminalCols();
        },
        get rows() {
          return terminalRows();
        },
        get scale() {
          return terminalScale();
        },
        get blink() {
          return state.blink;
        },
        get lines() {
          return state.lines;
        },
        get cursor() {
          return state.cursor;
        },
        get cursorHold() {
          return state.cursorHold;
        },
        get fontFamily() {
          return props.terminalFontFamily;
        },
        get lineHeight() {
          return props.terminalLineHeight;
        },
        ref(r$) {
          const _ref$3 = terminalRef;
          typeof _ref$3 === "function" ? _ref$3(r$) : terminalRef = r$;
        }
      }), null);
      insert(_el$2, createComponent(Show, {
        get when() {
          return props.controls !== false;
        },
        get children() {
          return createComponent(ControlBar, {
            get duration() {
              return duration();
            },
            get currentTime() {
              return state.currentTime;
            },
            get remainingTime() {
              return state.remainingTime;
            },
            get progress() {
              return state.progress;
            },
            markers: markers,
            get isPlaying() {
              return isPlaying();
            },
            get isPausable() {
              return state.isPausable;
            },
            get isSeekable() {
              return state.isSeekable;
            },
            onPlayClick: togglePlay,
            onFullscreenClick: toggleFullscreen,
            onHelpClick: toggleHelp,
            onSeekClick: seek,
            ref(r$) {
              const _ref$4 = controlBarRef;
              typeof _ref$4 === "function" ? _ref$4(r$) : controlBarRef = r$;
            }
          });
        }
      }), null);
      insert(_el$2, createComponent(Switch, {
        get children() {
          return [createComponent(Match, {
            get when() {
              return overlay() == "start";
            },
            get children() {
              return createComponent(StartOverlay, {
                onClick: play
              });
            }
          }), createComponent(Match, {
            get when() {
              return overlay() == "loader";
            },
            get children() {
              return createComponent(LoaderOverlay, {});
            }
          }), createComponent(Match, {
            get when() {
              return overlay() == "info";
            },
            get children() {
              return createComponent(InfoOverlay, {
                get message() {
                  return infoMessage();
                },
                get fontFamily() {
                  return props.terminalFontFamily;
                }
              });
            }
          }), createComponent(Match, {
            get when() {
              return overlay() == "error";
            },
            get children() {
              return createComponent(ErrorOverlay, {});
            }
          })];
        }
      }), null);
      insert(_el$2, createComponent(Show, {
        get when() {
          return isHelpVisible();
        },
        get children() {
          return createComponent(HelpOverlay, {
            get fontFamily() {
              return props.terminalFontFamily;
            },
            onClose: () => setIsHelpVisible(false),
            get isPausable() {
              return state.isPausable;
            },
            get isSeekable() {
              return state.isSeekable;
            }
          });
        }
      }), null);
      createRenderEffect(_p$ => {
        const _v$ = !!controlsVisible(),
          _v$2 = playerClass(),
          _v$3 = playerStyle();
        _v$ !== _p$._v$ && _el$.classList.toggle("ap-hud", _p$._v$ = _v$);
        _v$2 !== _p$._v$2 && className(_el$2, _p$._v$2 = _v$2);
        _p$._v$3 = style(_el$2, _v$3, _p$._v$3);
        return _p$;
      }, {
        _v$: undefined,
        _v$2: undefined,
        _v$3: undefined
      });
      return _el$;
    })();
    return el;
  });
  delegateEvents(["keydown", "mousemove"]);

  function mount(core, elem) {
    let opts = arguments.length > 2 && arguments[2] !== undefined ? arguments[2] : {};
    const metrics = measureTerminal(opts.terminalFontFamily, opts.terminalLineHeight);
    const props = {
      core: core,
      logger: opts.logger,
      cols: opts.cols,
      rows: opts.rows,
      fit: opts.fit,
      controls: opts.controls,
      autoPlay: opts.autoPlay,
      terminalFontSize: opts.terminalFontSize,
      terminalFontFamily: opts.terminalFontFamily,
      terminalLineHeight: opts.terminalLineHeight,
      theme: opts.theme,
      ...metrics
    };
    let el;
    const dispose = render(() => {
      el = createComponent(Player, props);
      return el;
    }, elem);
    return {
      el: el,
      dispose: dispose
    };
  }
  function measureTerminal(fontFamily, lineHeight) {
    const cols = 80;
    const rows = 24;
    const div = document.createElement("div");
    div.style.height = "0px";
    div.style.overflow = "hidden";
    div.style.fontSize = "15px"; // must match font-size of div.asciinema-player in CSS
    document.body.appendChild(div);
    let el;
    const dispose = render(() => {
      el = createComponent(Terminal, {
        cols: cols,
        rows: rows,
        lineHeight: lineHeight,
        fontFamily: fontFamily,
        lines: []
      });
      return el;
    }, div);
    const metrics = {
      charW: el.clientWidth / cols,
      charH: el.clientHeight / rows,
      bordersW: el.offsetWidth - el.clientWidth,
      bordersH: el.offsetHeight - el.clientHeight
    };
    dispose();
    document.body.removeChild(div);
    return metrics;
  }

  const CORE_OPTS = ['autoPlay', 'autoplay', 'cols', 'idleTimeLimit', 'loop', 'markers', 'pauseOnMarkers', 'poster', 'preload', 'rows', 'speed', 'startAt'];
  const UI_OPTS = ['autoPlay', 'autoplay', 'cols', 'controls', 'fit', 'rows', 'terminalFontFamily', 'terminalFontSize', 'terminalLineHeight', 'theme'];
  function coreOpts(inputOpts) {
    let overrides = arguments.length > 1 && arguments[1] !== undefined ? arguments[1] : {};
    const opts = Object.fromEntries(Object.entries(inputOpts).filter(_ref => {
      let [key] = _ref;
      return CORE_OPTS.includes(key);
    }));
    opts.autoPlay ??= opts.autoplay;
    opts.speed ??= 1.0;
    return {
      ...opts,
      ...overrides
    };
  }
  function uiOpts(inputOpts) {
    let overrides = arguments.length > 1 && arguments[1] !== undefined ? arguments[1] : {};
    const opts = Object.fromEntries(Object.entries(inputOpts).filter(_ref2 => {
      let [key] = _ref2;
      return UI_OPTS.includes(key);
    }));
    opts.autoPlay ??= opts.autoplay;
    opts.controls ??= "auto";
    return {
      ...opts,
      ...overrides
    };
  }

  function create(src, elem) {
    let opts = arguments.length > 2 && arguments[2] !== undefined ? arguments[2] : {};
    const logger = opts.logger ?? new DummyLogger();
    const core = new Core(src, coreOpts(opts, {
      logger
    }));
    const {
      el,
      dispose
    } = mount(core, elem, uiOpts(opts, {
      logger
    }));
    const ready = core.init();
    const player = {
      el,
      dispose,
      getCurrentTime: () => ready.then(core.getCurrentTime.bind(core)),
      getDuration: () => ready.then(core.getDuration.bind(core)),
      play: () => ready.then(core.play.bind(core)),
      pause: () => ready.then(core.pause.bind(core)),
      seek: pos => ready.then(() => core.seek(pos))
    };
    player.addEventListener = (name, callback) => {
      return core.addEventListener(name, callback.bind(player));
    };
    return player;
  }

  exports.create = create;

  return exports;

})({});
