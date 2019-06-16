(function ($) {
    'use strict';

    function GetAjax(route, actRow) {
        /// <summary>
        /// 一般呼叫API的ajax方法，可帶參數，此方法為同步執行，因此使用此方法時，請注意，若執行API執行過久，會造成手機功能卡住，直到API執行完畢
        /// </summary>
        /// <param name="route">str，API的路徑，範例：API檔案名稱/API功能名稱</param>
        /// <param name="actRow">str，傳給API的參數，範例：param=test&param2=test2</param>
        /// <param name="async">bit，設定是否為非同步</param>
        /// <returns>執行完畢後回傳API的結果，若resultCode為10，表示API執行成功，若為01表示API執行過程中有誤，若回傳connError表示連線失敗</returns>

        var returnData;
        $.ajax({
            url: "/api/" + route,
            type: "POST",
            dataType: "json",
            data: actRow,
            async: false,
            headers: {
                'Authorization': 'Basic ' + btoa('zhtech:24369238')
            },
            success: function (data) {
                returnData = data;
            },
            error: function (data) {
                returnData = "connError";
            }
        })
        return returnData;
    }


    function GetAjaxV2(route, actRow, async, returnData) {
        /// <summary>
        /// 一般呼叫API的ajax方法，該方法會出現Loading的dialog，但前提是async必須設定為true
        /// </summary>
        /// <param name="route">str，API的路徑，範例：API檔案名稱/API功能名稱</param>
        /// <param name="actRow">obj，傳給API的參數</param>
        /// <param name="async">bit，設定是否為非同步</param>
        /// <param name="returnData">obj，無須寫任何方法</param>
        /// <returns>執行完畢後回傳API的結果，若resultCode為10，表示API執行成功，若為01表示API執行過程中有誤，若回傳connError表示連線失敗</returns>

        $.ajax({
            url: "/api/" + route,
            type: "POST",
            dataType: "json",
            //contentType: 'application/json',
            data: actRow,
            //data: JSON.stringify(actRow),
            async: async,
            headers: {
                'Authorization': 'Basic ' + btoa('zhtech:24369238')
            },
            success: function (data) {
                returnData(data);
            },
            error: function (data) {
                returnData("connError");
            }
        })
    }


    function GetAjaxV3(route, actRow, async, returnData) {
        /// <summary>
        /// 一般呼叫API的ajax方法，該方法會出現Loading的dialog，但前提是async必須設定為true
        /// </summary>
        /// <param name="route">str，API的路徑，範例：API檔案名稱/API功能名稱</param>
        /// <param name="actRow">obj，傳給API的參數</param>
        /// <param name="async">bit，設定是否為非同步</param>
        /// <param name="returnData">obj，無須寫任何方法</param>
        /// <returns>執行完畢後回傳API的結果，若resultCode為10，表示API執行成功，若為01表示API執行過程中有誤，若回傳connError表示連線失敗</returns>

        $.ajax({
            url: "/api/" + route,
            type: "POST",
            dataType: "json",
            data: actRow,
            async: async,
            headers: {
                'Authorization': 'Basic ' + btoa('zhtech:24369238')
            },
            success: function (data) {
                returnData(data);
            },
            error: function (data) {
                returnData("connError");
            }
        })
    }


})(jQuery);