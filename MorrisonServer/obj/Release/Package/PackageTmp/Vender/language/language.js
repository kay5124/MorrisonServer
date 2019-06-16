var langs = [
    { 'value': 'en-US', 'text': 'Englis' },
    { 'value': 'zh-TW', 'text': '繁體中文' }
];

var htmlPage = '';

window.onload = function () {
    onDeviceReady();
}
//document.addEventListener('deviceready', onDeviceReady.bind(this), false);

function onDeviceReady() {
    languageInit();
};

function languageInit() {
    var currLanguage = localStorage.getItem('language');
    if (currLanguage == null || currLanguage == undefined || currLanguage == '') {
        currLanguage = langs[0]['value'];
    }

    var langJS = null;

    $.getJSON('/Vender/language/' + currLanguage + '.json', translate);
}

function translate(jsdata) {
    //var htmlPage = location.pathname.replace('/', '').replace('.html', '');
    var currUri = window.location.href;
    if (currUri.indexOf('?') > -1) {
        currUri = currUri.substr(0, currUri.indexOf('?'));
    }

    var start = currUri.lastIndexOf('/') + 1;
    var length = currUri.length;
    var currPage = currUri.substr(start, length - start);
    var arrPageData_shirp = currPage.split('#');
    var arrPageData = arrPageData_shirp[0].split('?');
    var htmlPage = arrPageData[0].replace('.html', '');

    var langData = jsdata[htmlPage];
    if (htmlPage == "T30") {
        for (var val in langData) {
            $('.' + val).html(langData[val]);
        }
    }
    else if (htmlPage == "T15") {
        for (var val in langData) {
            if (val == "T15_btn_View" || val == "T15_btn_Action") {
                $('.' + val).html(langData[val]);
            } else $('#' + val).html(langData[val]);
        }
    }
    else {
        for (var val in langData) {
            $('#' + val).html(langData[val]);
        }
    }
}