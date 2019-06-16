function OpenWindow(query, w, h, scroll) {
    var l = (screen.width - w) / 2;
    var t = (screen.height - h) / 2;

    winprops = 'resizable=0, height=' + h + ',width=' + w + ',top=' + t + ',left=' + l + 'w';
    if (scroll) winprops += ',scrollbars=1';
    // var f = window.open(query, "_blank", winprops);
    var f = window.open(query, "Report", "width=1020, height=720"); //,modal=yes
};

Date.prototype.yyyy_mm_dd = function () {

    var yyyy = this.getFullYear().toString();
    var mm = (this.getMonth() + 1).toString(); // getMonth() is zero-based         
    var dd = this.getDate().toString();

    //debugger
    //return yyyy + '-' + (mm[1] ? mm : "0" + mm[0]) + '-' + (dd[1] ? dd : "0" + dd[0]);
    return yyyy + '-' + (mm.length == 2 ? mm : "0" + mm) + '-' + (dd.length = 2 ? dd : "0" + dd);

};

Date.prototype.zhFormat = function (format) {
    var yyyy = this.getFullYear().toString();
    var MM = (this.getMonth() + 1).toString(); // getMonth() is zero-based         
    var dd = this.getDate().toString();
    var HH = this.getHours().toString();
    var mm = this.getMinutes().toString();
    var ss = this.getSeconds().toString();

    //debugger;
    switch (format) {
        case 'yyyy-MM-dd':
            return yyyy + '-' + (MM.length == 2 ? MM : "0" + MM) + '-' + (dd.length = 2 ? dd : "0" + dd);
            //break;
        case 'yyyy-MM-dd HH:mm':
            return yyyy + '-' + (MM.length == 2 ? MM : "0" + MM) + '-' + (dd.length = 2 ? dd : "0" + dd)
                + ' ' + (HH.length == 2 ? HH : "0" + HH) + ':' + (mm.length == 2 ? mm : "0" + mm);
            //break;
        case 'yyyy-MM-dd HH:mm:ss':
            return yyyy + '-' + (MM.length == 2 ? MM : "0" + MM) + '-' + (dd.length = 2 ? dd : "0" + dd)
                + ' ' + (HH.length == 2 ? HH : "0" + HH) + ':' + (mm.length == 2 ? mm : "0" + mm) + ':' + (ss.length == 2 ? ss : "0" + ss);
            //break;
        case 'HH:mm':
            return (HH.length == 2 ? HH : "0" + HH) + ':' + (mm.length == 2 ? mm : "0" + mm) ;
            //break;
        default:
            return yyyy + '-' + (mm.length == 2 ? mm : "0" + mm) + '-' + (dd.length = 2 ? dd : "0" + dd);
    }


    
};


$.fn.datebox.defaults.formatter = function (date) {
    // debugger
    var y = date.getFullYear();
    var m = date.getMonth() + 1;
    var d = date.getDate();
    return y + '-' + (m < 10 ? ('0' + m) : m) + '-' + (d < 10 ? ('0' + d) : d);
};

$.fn.datebox.defaults.parser = function (s) {
    // debugger
    if (!s) return new Date();
    var ss;
    if (s.indexOf("-") != -1) {
        ss = (s.split('-'));
    }
    if (s.indexOf("/") != -1) {
        ss = (s.split('/'));
    }

    if (!ss) return;

    var y = parseInt(ss[0], 10);
    var m = parseInt(ss[1], 10);
    var d = parseInt(ss[2], 10);
    if (!isNaN(y) && !isNaN(m) && !isNaN(d)) {
        return new Date(y, m - 1, d);
    } else {
        return new Date();
    }

};

//$.fn.datebox.defaults.parser = function (s) {
//     debugger
//    if (!s) return new Date();
//    var ss = (s.split('-'));
//    var y = parseInt(ss[0], 10);
//    var m = parseInt(ss[1], 10);
//    var d = parseInt(ss[2], 10);
//    if (!isNaN(y) && !isNaN(m) && !isNaN(d)) {
//        return new Date(y, m - 1, d);
//    } else {
//        return new Date();
//    }

//};

//#region validatebox -->selectOneValueCheck/selectValuesCheck

$.extend($.fn.validatebox.defaults.rules, {
    selectOneValueCheck: {
        validator: function (value, comboId) {
            //debugger;
            //var b1 = $(this).val();

            if (value == "Select" || value == "") {
                //var a = $(this);
                return false;

            }
            if (comboId != undefined) {
                //var selectVal = $("input[name=" + param[0] + "]").val();
                ////$("#msg").html(selectVal);
                //return parseInt(selectVal) > 0;
                var datas = $('#' + comboId).combobox('getData');
                var inputValue = $('#' + comboId).combobox('getText');

                var size = datas.length;
                //var flag_validate = false;

                for (var i = 0; i < size; i++) {
                    if (value == datas[i].text) {
                        return true;
                    }
                }
                return false;

            }
            else { return true; }
        },
        message: '請選擇'
    },

    selectValuesCheck: {
        validator: function (value, comboId) {
            //debugger;
            //var b1 = $(this).val();

            if (value == "Select" || value == "") {
                //var a = $(this);
                return false;

            }
            //if (comboId != undefined) {
            //    // var selectVal = $("input[name=" + comboId[0] + "]").val();

            //    var a = $('#' + comboId).combobox('getValues').length;

            //    var selectVal = $('#' + comboId).val();
            //    ////$("#msg").html(selectVal);
            //    //return parseInt(selectVal) > 0;
            //    return a > 0;
            //}

            return true;
        },
        message: '請選擇'
    },
    intOrFloat: {// 验证整数或小数
        validator: function (value) {
            return /^\d+(\.\d+)?$/i.test(value);
        },
        message: '請輸入整數或小數,並確保格式正確'
    },
    integer: {// 验证整数
        validator: function (value) {
            return /^[+]?[1-9]+\d*$/i.test(value);
        },
        message: '請輸入整數'
    },
    english: {// 验证英语
        validator: function (value) {
            return /^[A-Za-z]+$/i.test(value);
        },
        message: '請輸入英文'
    }
});

//#endregion

//#region Set Hot Key

//From 設定 熱鍵 以便操作
//alt+2:btnAdd alt+3:btnEdit    alt+4:btnDelete alt+5:btnSave   alt+6:btnCancel alt+8:btnQuery alt+e:btnExport alt+p:btnPrint
function hotKeyEvent2() {
    var oEvent = window.event;

    //alert(oEvent.ctrlKey + oEvent.keyCode);

    if (oEvent.altKey) {
        switch (oEvent.keyCode) {
            case 50://alt+2
                {
                    try {
                        btnAdd();
                    }
                    catch (err) {
                        //document.getElementById("demo").innerHTML = err.message;
                    }
                }
                break;
            case 51://alt+3
                {
                    try {
                        btnEdit();
                    }
                    catch (err) {
                        //document.getElementById("demo").innerHTML = err.message;
                    }
                }
                break;
            case 52://alt+4
                {
                    try {
                        btnDelete();
                    }
                    catch (err) {
                        //document.getElementById("demo").innerHTML = err.message;
                    }
                }
                break;
            case 53://alt+5
                {
                    try {

                        btnSave();

                        //if (RowStatus == "A" || RowStatus == "M") {
                        //    btnSave();
                        //}
                    }
                    catch (err) {
                        //document.getElementById("demo").innerHTML = err.message;
                    }

                }
                break;
            case 54://alt+6
                {
                    try {
                        btnCancel();
                    }
                    catch (err) {
                        //document.getElementById("demo").innerHTML = err.message;
                    }

                }
                break;
            case 56://alt+8
                {
                    btnQuery();
                }
                break;

            case 69://alt+e
                {
                    try {
                        btnExport();
                    }
                    catch (err) {
                        //document.getElementById("demo").innerHTML = err.message;
                    }
                }
                break;
            case 80://alt+p
                {
                    try {
                        btnPrint();
                    }
                    catch (err) {
                        //document.getElementById("demo").innerHTML = err.message;
                    }
                }
                break;

        }

    }

    //if (oEvent.keyCode == 49 && oEvent.altKey) {
    //    alert("你按下了ctrl+1");
    //}
}

//#endregion

