var cainao_socket = null;
var cainao_socket_open = 0;
var cainao_succeeCallback = null;
var cainao_errorCallback = null;
var cainao_p_count = 0;

function PrintShippingLabel(host, taskArray, callback, errorCallback) {
    var ztoExpressSheets = [];
    var cainiaoExpressSheets = [];
    var yundaExpressSheets = [];
    var totalCount = (taskArray || []).length;
    var returnObj = { Success: true, SuccessCount: 0, Msg: '', SuccessOrderNos: [], ErrorMsg: '' };
    taskArray.forEach(function (item) {
        if (item.ExpressCompanyCode.indexOf('CaiNiao') >= 0) {
            cainiaoExpressSheets.push(item);
        }
        else if (item.ExpressCompanyCode === 'ZTO') {
            ztoExpressSheets.push(item);
        }
        else if (item.ExpressCompanyCode === 'YUNDA') {
            yundaExpressSheets.push(item);
        }
        else {
            returnObj.Success = false;
            returnObj.Msg = '仓库订单号：' + item.WHOrderNo + '暂不支持快递公司为"' + item.ExpressCompanyCode + '"的打印';
            errorCallback(returnObj);
            return;
        }
    });
    if (ztoExpressSheets && ztoExpressSheets.length > 0 && returnObj.Success == true) {
        PrintZTOShippingLabel(host, ztoExpressSheets,
            function (res) {
                res.SuccessOrderNos.forEach(function (item) {
                    returnObj.SuccessOrderNos.push(item);
                });
                PrintMsgCallBack(returnObj, totalCount, callback, errorCallback);
            },
            function (res) {
                returnObj.ErrorMsg = res.ErrorMsg || '';
                returnObj.Success = false;
                PrintMsgCallBack(returnObj, totalCount, callback, errorCallback);
            });
    }
    if (cainiaoExpressSheets && cainiaoExpressSheets.length > 0 && returnObj.Success == true) {
        var index = 0;
        CaiNiaoPrint(returnObj, cainiaoExpressSheets, index, totalCount ,callback, errorCallback);
    }
    if (yundaExpressSheets && yundaExpressSheets.length > 0 && returnObj.Success == true) {
        var index = 0;
        PrintYunDaShippingLabel(returnObj, yundaExpressSheets, totalCount, callback, errorCallback);
    }
}
function PrintMsgCallBack(returnObj, totalCount, callback, errorCallback) {
    var successCount = (returnObj.SuccessOrderNos || []).length;
    var failCount = totalCount - successCount;
    returnObj.Msg = "打印成功数量：" + successCount + "，未打印数量：" + failCount + "，打印失败消息：" + returnObj.ErrorMsg;
    if (!returnObj.Success) {
        errorCallback(returnObj);
    }
    else {
        if (successCount >= totalCount) {
            callback(returnObj);
        }
    }
}

function PrintYunDaShippingLabel(returnObj, taskArray, totalCount, callback, errorCallback) {
    if (taskArray === null || taskArray.length <= 0) {
        return;
    }
    var data = taskArray.pop();
    if (!data) {
        returnObj.ErrorMsg = '待打印韵达面单的订单列表里出现了空数据';
        returnObj.Success = false;
        PrintMsgCallBack(returnObj, totalCount, callback, errorCallback);
        return;
    }
    var printData = data.CaiNiaoPrintData;
    if (!printData || printData === null) {
        returnObj.Msg = '仓库订单编号：' + data.WHOrderNo + '的打印内容（ExtensionFields字段）不能为空';
        returnObj.Success = false;
        PrintMsgCallBack(returnObj, totalCount, callback, errorCallback);
        return;
    }
    var host = "127.0.0.1:9090";
    var url = 'http://' + host + '/ydecx/service/mailpx/printDirect.pdf?t=' + new Date().getTime();
    $.ajax({
        type: 'GET',
        url: url,
        data: { tname: 'mailtmp_s12', docname: 'mailpdfm1', value: printData },
        dataType: 'text',
        contentType: 'application/x-www-form-urlencoded',
        success: function (r) {
            //if (!r.error) {
            if (r && $.trim(r) == 'success') {
                returnObj.SuccessOrderNos.push(data.WHOrderNo);
                PrintMsgCallBack(returnObj, totalCount, callback, errorCallback);
                PrintYunDaShippingLabel(returnObj, taskArray, totalCount, callback, errorCallback);
            }
            else {
                returnObj.ErrorMsg = r;
                returnObj.Success = false;
                PrintMsgCallBack(returnObj, totalCount, callback, errorCallback);
            }
        }
    });
}

function CaiNiaoPrint(returnObj, taskArray, index, totalCount, callback, errorCallback) {
    if (!taskArray || taskArray.length <= 0) {
        return;
    }
    var len = taskArray.length;
    if (index > len) {
        return;
    }
    var data = taskArray[index];
    if (!data) {
        returnObj.ErrorMsg = '待打印菜鸟面单的订单列表里出现了空数据';
        returnObj.Success = false;
        PrintMsgCallBack(returnObj, totalCount, callback, errorCallback);
        return;
    }
    var printData = data.CaiNiaoPrintData;
    if (!printData || printData === null) {
        returnObj.Msg = '仓库订单编号：' + data.WHOrderNo + '的打印内容（ExtensionFields字段）不能为空';
        returnObj.Success = false;
        PrintMsgCallBack(returnObj, totalCount, callback, errorCallback);
        return;
    }
    var printerName = GetCaiNiaoPrinterName(data.ExpressCompanyCode);
    PrintCaiNiaoShippingLabel(printerName, data.CaiNiaoPrintData, function () {
        returnObj.SuccessOrderNos.push(data.WHOrderNo);
        PrintMsgCallBack(returnObj, totalCount, callback, errorCallback);
        index++;
        CaiNiaoPrint(returnObj, taskArray, index, totalCount, callback, errorCallback);
    }, function (msg) {
        returnObj.ErrorMsg = msg;
        returnObj.Success = false;
        PrintMsgCallBack(returnObj, totalCount, callback, errorCallback);
    });
}
function GetCaiNiaoPrinterName(expressCompanyCode) {
    var printerName = '';
    if (expressCompanyCode.indexOf('YUNDA') >= 0) {
        printerName = 'YUNDA_Printer';//YUNDA_Printer
    }
    return printerName;
}
function PrintZTOShippingLabel(host, taskArray, callback, errorCallback) {
    if (!taskArray || taskArray === null) {
        IBBAlert.Error('打印内容不能为空');
        return;
    }
    var url = 'http://' + host + '/api/printer/printall';
    $.ajax({
        type: 'POST',
        data: JSON.stringify(taskArray),
        dataType: 'JSON',
        contentType: 'application/json',
        url: url,
        success: function (res) {
            if (res.Success === true) {
                if (callback && typeof (callback) === 'function') {
                    callback(res);
                }
            }
            else {
                if (errorCallback && typeof (errorCallback) === 'function') {
                    errorCallback(res);
                }
            }
        }
    });
}

function PrintCaiNiaoShippingLabel(printerName, taskArray, callback, errorCallback) {
    if (!printerName || printerName === null || typeof (printerName) !== 'string') {
        printerName = ''; // 使用默认打印机
    }
    if (!taskArray || taskArray === null) {
        errorCallback('打印内容不能为空');
        return;
    }
    try {
        var docs;
        if (typeof (taskArray) === 'string') {
            var tmp = eval("(" + taskArray + ")");
            if (Object.prototype.toString.call(tmp) === '[object Array]') {
                docs = tmp;
            }
            else {
                docs = [tmp]; // 变成数组
            }
        }
        else if (Object.prototype.toString.call(taskArray) === '[object Array]') {
            var ary = [];
            for (var i = 0; i < taskArray.length; i++) { // 检查数组里的每一个元素
                var obj = taskArray[i];
                if (typeof (obj) === 'string') { // 如果元素是字符串，则说明是json字符串，转换为obj对象再放入数组
                    ary.push(eval("(" + obj + ")"));
                }
                else { // 如果元素非字符串，那么说明就是对象了，直接放入数组
                    ary.push(obj);
                }
            }
            docs = ary;
        }
        else {
            docs = [taskArray]; // 变成数组
        }
    }
    catch (err) {
        errorCallback(err);
        return;
    }
    var len = (docs || []).length;
    if (callback && typeof (callback) === 'function') {
        cainao_succeeCallback = function (c) { if (c === len) callback(); };
    }
    cainao_errorCallback = errorCallback;
    var request = {
        cmd: "print",
        requetID: "12345678901234567890",
        version: "1.0",
        task: {
            taskID: _generateRandomTaskID(3),
            preview: false,
            printer: printerName,
            documents: docs
        }
    };
    _doConnectCaiNiaoPrinter(function (socket) {
        socket.send(JSON.stringify(request));
    });
}

function _doConnectCaiNiaoPrinter(callback) {
    if (cainao_socket && cainao_socket.readyState === 1) {
        callback(cainao_socket);
        return;
    }
    var cainao_socket_open = 0;
    cainao_socket = new WebSocket('ws://127.0.0.1:13528');
    cainao_socket.onerror = function (event) {
        cainao_errorCallback('websocket连接readyState=' + event.currentTarget.readyState);
    };
    // 打开Socket
    cainao_socket.onopen = function (event) {
        // 监听消息
        cainao_socket.onmessage = function (event) {
            var d = event.data;
            if (d) {
                var obj = eval("(" + d + ")");
                var status = obj.status;
                var msg = obj.msg;
                if (status === 'failed') {
                    if (cainao_errorCallback && typeof (cainao_errorCallback) === 'function') {
                        cainao_errorCallback("打印失败：" + msg + "<br/><br/>返回：" + d);
                    }
                }
                else {
                    if (obj.cmd === 'notifyPrintResult') {
                        cainao_p_count++;
                        if (cainao_succeeCallback && typeof (cainao_succeeCallback) === 'function') {
                            cainao_succeeCallback(cainao_p_count);
                        }
                    }
                }
            }
            console.log('Client received a message', event);
        };
        // 监听Socket的关闭
        cainao_socket.onclose = function (event) {
            console.log('Client notified socket has closed', event);
            cainao_socket = null;
            cainao_socket_open = 0;
        };
        cainao_socket_open = 1;
        callback(cainao_socket);
    };
}

function _generateRandomTaskID(l) {
    var x = "0123456789qwertyuioplkjhgfdsazxcvbnm";
    var tmp = "";
    var timestamp = new Date().getTime();
    for (var i = 0; i < l; i++) {
        tmp += x.charAt(Math.ceil(Math.random() * 100000000) % x.length);
    }
    return timestamp + tmp;
}


function packagesBatchReprint(btn, packageIds) {
    IBBAlert.Confirm('确认打印以下选中包裹的面单吗',
        function (y) {
            if (y === false) {
                return;
            }
            btn.button('loading');
            $.ajax({
                type: 'POST',
                dataType: 'json',
                url: "/SO/AjaxBatchRePrintByPackageIds",
                data: {
                    packageIds: packageIds
                },
                success: function (data) {
                    if (!data.error) {
                        var returnData = data.data;
                        if (!returnData.length || returnData.length === 0) {
                            IBBAlert.Error("没有可以打印的包裹数据");
                            btn.button('reset');
                            return;
                        }
                        toastr.success('操作成功,准备打印面单...');
                        PrintShippingLabel('localhost:18999',
                            returnData,
                            function (res) {
                                IBBAlert.Success(res.Msg);
                            },
                            function (res) {
                                IBBAlert.Error(res.Msg);
                                btn.button('reset'
                                );
                            });
                    }
                    btn.button('reset');
                },
                error: function (ex) {
                    IBBAlert.Error(ex);
                    btn.button('reset');
                }
            });
        });
}
function soReprint(btn, soId, packageId) {
    IBBAlert.Confirm('确认打印印面单吗',
        function (y) {
            if (y === false) {
                return;
            }
            btn.button('loading');
            $.ajax({
                type: 'POST',
                dataType: 'json',
                url: "/SO/AjaxRePrint",
                data: {
                    soId: soId,
                    packageId: packageId
                },
                success: function (data) {
                    if (!data.error) {
                        var returnData = data.data;
                        if (!returnData.length || returnData.length === 0) {
                            IBBAlert.Error("没有可以打印的包裹数据");
                            btn.button('reset');
                            return;
                        }
                        toastr.success('操作成功,准备打印面单...');
                        PrintShippingLabel('localhost:18999',
                            returnData,
                            function (res) {
                                IBBAlert.Success(res.Msg);
                            },
                            function (res) {
                                IBBAlert.Error(res.Msg);
                                btn.button('reset'
                                );
                            });
                    }
                    btn.button('reset');
                },
                error: function (ex) {
                    IBBAlert.Error(ex);
                    btn.button('reset');
                }
            });
        });
}