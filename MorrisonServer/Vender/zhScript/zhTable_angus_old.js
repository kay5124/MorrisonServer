var _totalCount = 0;     //總筆數
var _page = 1;      //目前的頁數
var _rows = 10;       //顯示資料筆數
var _totalPage1 = 0; //總頁數
var _order = '';

//改變頁碼
function btnChangePage(selPage) {
    _page = selPage;
    btnQuery('');
}


//動態建立頁碼
function genPage(totalPage) {
    var dg = '';

    dg += '<li class="page-item disabled" id="pagePrevious">';
    dg += '    <a class="page-link" href="#" tabindex="-1" id="pagePrevious_a">Previous</a>';
    dg += '</li>';
    if (totalPage > _rows) {
        if (_page < 7) {
            for (var i = 1 ; i <= 9 ; i++) {
                if (i == _page) {
                    dg += '<li class="page-item active" name="pageNum"><a class="page-link" href="#" id="page' + i + '" onclick="btnChangePage(' + i + ')">' + i + '</a></li>';
                    //document.getElementById("divButton").innerHTML += '<button id="page' + i + '" class="button btnGray" onclick="btnChangePage(' + i + ')" style="background-color:silver">' + i + '</button>';
                } else {
                    dg += '<li class="page-item" name="pageNum"><a class="page-link" href="#" id="page' + i + '" onclick="btnChangePage(' + i + ')">' + i + '</a></li>';
                    //document.getElementById("divButton").innerHTML += '<button id="page' + i + '" class="button btnGray" onclick="btnChangePage(' + i + ')">' + i + '</button>';
                }
            }
            dg += '<li class="page-item"><a class="page-link" href="#">…</a></li>';
            dg += '<li class="page-item" name="pageNum"><a class="page-link" href="#" id="page' + totalPage + '" onclick="btnChangePage(' + totalPage + ')">' + totalPage + '</a></li>';
            //document.getElementById("divButton").innerHTML += '…<button id="page' + totalPage + '" class="button btnGray" onclick="btnChangePage(' + totalPage + ')">' + totalPage + '</button>';
        } else if ((_page + 6) > totalPage) {
            dg += '<li class="page-item" name="pageNum"><a class="page-link" href="#" id="page' + totalPage + '" onclick="btnChangePage(1)">1</a></li>';
            //document.getElementById("divButton").innerHTML += '<button id="page' + totalPage + '" class="button btnGray" onclick="btnChangePage(1)">1</button>…';
            for (var i = totalPage - 10; i <= totalPage; i++) {
                if (i == _page) {
                    dg += '<li class="page-item active" name="pageNum"><a class="page-link" href="#" id="page' + i + '" onclick="btnChangePage(' + i + ')">' + i + '</a></li>';
                    //document.getElementById("divButton").innerHTML += '<button id="page' + i + '" class="button btnGray" onclick="btnChangePage(' + i + ')" style="background-color:silver">' + i + '</button>';
                } else {
                    dg += '<li class="page-item" name="pageNum"><a class="page-link" href="#" id="page' + i + '" onclick="btnChangePage(' + i + ')">' + i + '</a></li>';
                    //document.getElementById("divButton").innerHTML += '<button id="page' + i + '" class="button btnGray" onclick="btnChangePage(' + i + ')">' + i + '</button>';
                }
            }
        } else {
            dg += '<li class="page-item" name="pageNum"><a class="page-link" href="#" id="page' + totalPage + '" onclick="btnChangePage(1)">1</a></li>';
            //document.getElementById("divButton").innerHTML += '<button id="page' + totalPage + '" class="button btnGray" onclick="btnChangePage(1)">1</button>…';
            for (var i = _page - 4 ; i <= _page + 4 ; i++) {
                if (i == _page) {
                    dg += '<li class="page-item active" name="pageNum"><a class="page-link" href="#" id="page' + i + '" onclick="btnChangePage(' + i + ')">' + i + '</a></li>';
                    //document.getElementById("divButton").innerHTML += '<button id="page' + i + '" class="button btnGray" onclick="btnChangePage(' + i + ')" style="background-color:silver">' + i + '</button>';
                } else {
                    dg += '<li class="page-item" name="pageNum"><a class="page-link" href="#" id="page' + i + '" onclick="btnChangePage(' + i + ')">' + i + '</a></li>';
                    //document.getElementById("divButton").innerHTML += '<button id="page' + i + '" class="button btnGray" onclick="btnChangePage(' + i + ')">' + i + '</button>';
                }
            }
            dg += '<li class="page-item"><a class="page-link" href="#">…</a></li>';
            dg += '<li class="page-item" name="pageNum"><a class="page-link" href="#" id="page' + totalPage + '" onclick="btnChangePage(' + totalPage + ')">' + totalPage + '</a></li>';
            //document.getElementById("divButton").innerHTML += '…<button id="page' + totalPage + '" class="button btnGray" onclick="btnChangePage(' + totalPage + ')">' + totalPage + '</button>';
        }
    } else {
        for (var i = 1 ; i <= totalPage ; i++) {
            if (i == _page) {
                dg += '<li class="page-item active" name="pageNum"><a class="page-link" href="#" id="page' + i + '" onclick="btnChangePage(' + i + ')">' + i + '</a></li>';
                //document.getElementById("divButton").innerHTML += '<button id="page' + i + '" class="button btnGray" onclick="btnChangePage(' + i + ')" style="background-color:silver">' + i + '</button>';
            } else {
                dg += '<li class="page-item" name="pageNum"><a class="page-link" href="#" id="page' + i + '" onclick="btnChangePage(' + i + ')">' + i + '</a></li>';
                //document.getElementById("divButton").innerHTML += '<button id="page' + i + '" class="button btnGray" onclick="btnChangePage(' + i + ')">' + i + '</button>';
            }
        }
    }
    dg += '<li class="page-item" id="pageNext">';
    dg += '    <a class="page-link" href="#" id="pageNext_a">Next</a>';
    dg += '</li>';

    $('#ulPage').html(dg);


    if (_page == 1) {
        $('#pagePrevious').addClass('disabled');
        if (totalPage == 1) {
            $('#pageNext').addClass('disabled');
        } else {
            $('#pageNext').removeClass('disabled');
        }
    } else if (totalPage == _page) {
        $('#pagePrevious').removeClass('disabled');
        $('#pageNext').addClass('disabled');
    } else {
        $('#pagePrevious').removeClass('disabled');
        $('#pageNext').removeClass('disabled');
    }

    $('#pagePrevious').on('click', function () {
        if ($('#pagePrevious').hasClass('disabled')) {
            return;
        }
        btnChangePage(_page - 1);
    });

    $('#pageNext').on('click', function () {
        if ($('#pageNext').hasClass('disabled')) {
            return;
        }
        btnChangePage(_page + 1);
    });
}


function changePage() {
    if (isNaN($("#txtPage").val())) {
        alert("頁碼必須為數字");
    } else if ($("#txtPage").val() > _totalPage1) {
        alert("您輸入的頁碼大於目前的總頁數！");
    } else if ($("#txtPage").val() < 1) {
        alert("您輸入的頁碼大於目前的總頁數！");
    } else {
        _page = parseInt($("#txtPage").val());
        btnQuery('');
    }
}


function btnSort(elementId) {
    //▼= &#9660; 
    //▲= &#9650;

    $('span[name="_order"]').html('');

    _order = $('#_' + elementId).attr('order');
    if (_order == 'desc') {
        _order = elementId + ' asc';
        $('#_' + elementId).attr('order', 'asc');
        $('#order_' + elementId).html('&#9650;');
    } else {
        _order = elementId + ' desc';
        $('#_' + elementId).attr('order', 'desc');
        $('#order_' + elementId).html('&#9660;');
    }

    btnQuery('');
}