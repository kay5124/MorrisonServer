(function ($) {
    $.zhTable = function zhTable(options, callback, element) {
        this.element = $(element);
        this._init(options, callback);
    }

    $.zhTable.defaults = {
        _totalCount: 0,     //總筆數
        _page: 1,      //目前的頁數
        _rows: 20,       //顯示資料筆數
        _totalPage1: 0, //總頁數
        _order: '',
        _columns: new Array()
    }

    $.zhTable.prototype = {
        _init: function zhTable_init(options, callback) {
            var instance = this, opts = this.options = $.extend(true, {}, $.zhTable.defaults, options);

            // 開始做事
            if (options._columns.length > 0) {
                var dg = '';

                var _divId = instance.element[0].id;

                dg += '<table class="rwd-table" id="tbl_' + instance.element[0].id + '">';
                dg += '    <tr>';
                var arrColumns = options._columns;
                for (var i = 0 ; i < arrColumns.length; i++) {
                    if (arrColumns[i].sort) {
                        switch (arrColumns[i].type) {
                            case 'checkbox':
                                dg += '<th name="th_' + instance.element[0].id + '" col="' + arrColumns[i].id + '" orderby="' + i + '" type="' + arrColumns[i].type + '" ' + customizePropToString(arrColumns[i].customizeProp) + ' valueKey="' + arrColumns[i].valueKey + '" ><input name="columnChk" col="' + arrColumns[i].id + '" type="checkbox" /></th>';
                                break;
                            default:
                                dg += '<th name="th_' + instance.element[0].id + '" col="' + arrColumns[i].id + '" orderby="' + i + '" type="' + arrColumns[i].type + '" ' + customizePropToString(arrColumns[i].customizeProp) + ' ><a href="#" id="tbl_' + instance.element[0].id + '_' + arrColumns[i].id + '" order="desc" name="tbl_' + instance.element[0].id + '_order">' + arrColumns[i].text + '<span id="order_tbl_' + instance.element[0].id + '_' + arrColumns[i].id + '" name="_order_' + instance.element[0].id + '"></span></a></th>';
                                break;
                        }
                    } else {
                        switch (arrColumns[i].type) {
                            case 'checkbox':
                                dg += '<th name="th_' + instance.element[0].id + '" col="' + arrColumns[i].id + '" orderby="' + i + '" type="' + arrColumns[i].type + '" ' + customizePropToString(arrColumns[i].customizeProp) + ' valueKey="' + arrColumns[i].valueKey + '" ><input name="columnChk" col="' + arrColumns[i].id + '" type="checkbox" /></th>';
                                break;
                            default:
                                dg += '<th name="th_' + instance.element[0].id + '" col="' + arrColumns[i].id + '" orderby="' + i + '" type="' + arrColumns[i].type + '" ' + customizePropToString(arrColumns[i].customizeProp) + ' >' + arrColumns[i].text + '</th>';
                                break;
                        }
                    }
                }
                dg += '    </tr>';
                dg += '</table>';
                dg += '<br />';
                dg += '<div class="col-md-3" id="divRowCount_' + instance.element[0].id + '" style="font-family:Microsoft JhengHei"></div>';
                dg += '<div class="col-md-9" id="divButton_' + instance.element[0].id + '" style="font-family:Microsoft JhengHei;text-align:right;">';
                dg += '    <nav aria-label="Page navigation example">';
                dg += '        <ul class="pagination justify-content-center" style="margin-top:0px;" id="ulPage_' + instance.element[0].id + '"></ul>';
                dg += '    </nav>';
                dg += '</div>';

                $('#' + instance.element[0].id).html(dg);

                $('a[name="tbl_' + instance.element[0].id + '_order"]').on('click', function () {
                    btnSort(this.id, _divId);
                });

                $('input[name="columnChk"]').on('change', function () {
                    var chk = this.checked;
                    var chk_d = $(this).attr('col') + '_d';
                    $('input[name="' + chk_d + '"]').attr('checked', chk);
                })
            }

            // 最後回呼，如果你不要就把 callback 拿掉就好
            if ($.isFunction(callback)) callback(this);
        },
        loadData: function zhTable_loadData(options, reload) {
            var _divId = this.element[0].id;
           
            if (!reload) {
                this.options._page = 1;
                this.options._order = "";
                $('span[name="_order_' + _divId + '"]').html('');
            }
            var _rows = this.options._rows;
            var _page = this.options._page;
            var _totalPage1 = this.options._totalPage1;

            this.options['extendParam'] = options;

            var actRow = {
                "page": this.options._page,
                "rows": this.options._rows,
                'order': this.options._order
            };

            var actRow = Object.assign(options, actRow);

            $.ajax({
                url: this.options.url,
                type: 'POST',
                data: actRow,
                ajax: false,
                success: function (data) {
                    if (data.resultCode == "10") {
                        $('#tbl_' + _divId + ' tr').eq(0).nextAll().remove();
                        if (data.rows.length > 0) {
                            var arrTh = $('th[name="th_' + _divId + '"]');
                            for (var i = 0 ; i < data.rows.length; i++) {
                                var row = data.rows[i];
                                var dg = '';
                                dg += '<tr>';
                                for (var j = 0 ; j < arrTh.length; j++) {
                                    switch ($(arrTh[j]).attr('type')) {
                                        case 'checkbox':
                                            dg += '    <td data-th="' + $(arrTh[j]).text() + '"><input type="checkbox" id="" value="' + row[$(arrTh[j]).attr('valueKey')] + '" name="' + $(arrTh[j]).attr('col') + '_d" ' + (row[$(arrTh[j]).attr('col')] ? 'checked="checked"' : '') + ' /></td>';
                                            break;
                                        default:
                                            dg += '    <td data-th="' + $(arrTh[j]).text() + '" ' + customizePropToTdProp(row, $(arrTh[j]), $(arrTh[j]).attr('customizePropNum')) + ' >' + row[$(arrTh[j]).attr('col')] + '</td>';
                                            break;
                                    }
                                }
                                dg += '</tr>';
                                $("#tbl_" + _divId).append(dg);
                            }

                            document.getElementById("divRowCount_" + _divId).innerHTML = "Total：" + data.totalCount;
                            genPage(data.totalPage, _rows, _page, _divId);//動態建立頁碼
                            _totalPage1 = data.totalPage;

                        } else {
                            swal("", 'Not found', "warning");
                            //alert("查無任何資料");
                            document.getElementById("divRowCount_" + _divId).innerHTML = "Total：0";
                            genPage(0, _rows, _page, _divId);//動態建立頁碼
                            _totalPage1 = 0;
                        }
                    } else {
                        swal("", data.error, "warning");
                    }
                }
            })
        },
        reload: function zhTable_reload(options) {
            if (options != undefined && options != null && options != '' && options != '{}')
                this.options['extendParam'] = options;
            $('#' + this.element[0].id).zhTable('loadData', this.options['extendParam'], true);
        },
        changePage: function zhTable_changePage(action) {
            if (action == 'next') {
                this.options._page++;
            } else if(action == 'pre') {
                this.options._page--;
            } else {
                this.options._page = action;
            }

            $('#' + this.element[0].id).zhTable('reload', this.options['extendParam']);
        },
        sort: function zhTable_sort(order) {
            this.options._order = order;
            $('#' + this.element[0].id).zhTable('reload', '');
        },
        getJSONData: function zhTable_getJSONData(options, reload, callback) {
            var _divId = this.element[0].id;

            if (!reload) {
                this.options._page = 1;
                this.options._order = "";
                $('span[name="_order_' + _divId + '"]').html('');
            }
            var _rows = this.options._rows;
            var _page = this.options._page;
            var _totalPage1 = this.options._totalPage1;

            this.options['extendParam'] = options;

            var actRow = {
                "page": this.options._page,
                "rows": this.options._rows,
                'order': this.options._order
            };

            var actRow = Object.assign(options, actRow);

            $.ajax({
                url: this.options.url,
                type: 'POST',
                data: actRow,
                ajax: false,
                success: function (data) {
                    if (data.resultCode == "10") {
                        callback(data.rows);
                    } else {
                        swal("Error", data.error, "warning");
                        callback('');
                    }
                }
            })
        }
    }

    $.fn.zhTable = function (options, callback) {

        var thisCall = typeof options;

        switch (thisCall) {
            // method
            case 'string':
                var args = Array.prototype.slice.call(arguments, 1);

                this.each(function () {
                    var instance = $.data(this, 'zhTable');

                    if (!instance) {
                        return false;
                    }
                    // 我們使用 _ 開頭的函式來當作私有函式，所以不允許由外部呼叫
                    if (!$.isFunction(instance[options]) || options.charAt(0) === "_") {
                        return false;
                    }

                    instance[options].apply(instance, args);
                });
                break;

                // creation
            case 'object':
                this.each(function () {
                    var instance = $.data(this, 'zhTable');

                    if (instance) {
                        instance.update(options);
                    } else {
                        // 我們透過 new 來動態建立一個我們所寫好的 prototype
                        // 並且將他利用 $.data 的方式儲存起來
                        // 好處是，我們隨時都可以用 $.data 把他拿出來作壞事
                        $.data(this, 'zhTable', new $.zhTable(options, callback, this));
                    }
                });
                break;
        }

        return this;
    };

    function customizePropToString(customizeProp) {

        var tmp = '';
        if (customizeProp != undefined && customizeProp != null && customizeProp != '') {
            var i = 0;
            if (typeof (customizeProp) == 'object') {
                for (var key in customizeProp) {
                    i++;
                    tmp += 'customizeProp' + i + '="' + customizeProp[key] + '" ';
                }

                tmp += 'customizePropNum="' + i + '" ';
            } else {
                tmp = 'customizeProp="' + customizeProp + '" ';
                tmp += 'customizePropNum="1" ';
            }
        }

        return tmp;
    }

    function customizePropToTdProp(row, element, customizePropNum) {
        if (customizePropNum == undefined || customizePropNum == null || customizePropNum == '') return;

        var tmp = '';
        for (var i = 1; i <= customizePropNum; i++) {
            tmp += element.attr('customizeProp' + i) + '="' + row[element.attr('customizeProp' + i)].trim() + '" ';
        }
        return tmp;
    }

    function genPage(totalPage, _rows, _page, _divId) {
        var dg = '';

        dg += '<li class="page-item" id="pagePrevious">';
        dg += '    <a class="page-link btn" href="#" tabindex="-1" id="pagePrevious_' + _divId + '">Pre</a>';
        dg += '</li>';
        if (totalPage > 10) {
            if (_page < 7) {
                for (var i = 1 ; i <= 9 ; i++) {
                    if (i == _page) {
                        dg += '<li class="page-item active" name="pageNum"><a class="page-link" href="#" id="page_' + _divId + '_' + i + '" name="btnChangePage">' + i + '</a></li>';
                    } else {
                        dg += '<li class="page-item" name="pageNum"><a class="page-link" href="#" id="page_' + _divId + '_' + i + '" name="btnChangePage">' + i + '</a></li>';
                    }
                }
                dg += '<li class="page-item"><a class="page-link" href="#">…</a></li>';
                dg += '<li class="page-item" name="pageNum"><a class="page-link" href="#" id="page' + totalPage + '" onclick="btnChangePage(' + totalPage + ')">' + totalPage + '</a></li>';
            } else if ((_page + 6) > totalPage) {
                dg += '<li class="page-item" name="pageNum"><a class="page-link" href="#" id="page' + totalPage + '" name="btnChangePage">1</a></li>';
                for (var i = totalPage - 10; i <= totalPage; i++) {
                    if (i == _page) {
                        dg += '<li class="page-item active" name="pageNum"><a class="page-link" href="#" id="page_' + _divId + '_' + i + '" name="btnChangePage">' + i + '</a></li>';
                    } else {
                        dg += '<li class="page-item" name="pageNum"><a class="page-link" href="#" id="page_' + _divId + '_' + i + '" name="btnChangePage">' + i + '</a></li>';
                    }
                }
            } else {
                dg += '<li class="page-item" name="pageNum"><a class="page-link" href="#" id="page' + totalPage + '" name="btnChangePage">1</a></li>';
                dg += '<li class="page-item"><a class="page-link" href="#">…</a></li>';
                for (var i = _page - 4 ; i <= _page + 4 ; i++) {
                    if (i == _page) {
                        dg += '<li class="page-item active" name="pageNum"><a class="page-link" href="#" id="page_' + _divId + '_' + i + '" name="btnChangePage">' + i + '</a></li>';
                    } else {
                        dg += '<li class="page-item" name="pageNum"><a class="page-link" href="#" id="page_' + _divId + '_' + i + '" name="btnChangePage">' + i + '</a></li>';
                    }
                }
                dg += '<li class="page-item"><a class="page-link" href="#">…</a></li>';
                dg += '<li class="page-item" name="pageNum"><a class="page-link" href="#" id="page' + totalPage + '" name="btnChangePage">' + totalPage + '</a></li>';
            }
        } else {
            for (var i = 1 ; i <= totalPage ; i++) {
                if (i == _page) {
                    dg += '<li class="page-item active" name="pageNum"><a class="page-link" href="#" id="page_' + _divId + '_' + i + '" name="btnChangePage">' + i + '</a></li>';
                } else {
                    dg += '<li class="page-item" name="pageNum"><a class="page-link" href="#" id="page_' + _divId + '_' + i + '" name="btnChangePage">' + i + '</a></li>';
                }
            }
        }
        dg += '<li class="page-item" id="pageNext">';
        dg += '    <a class="page-link btn" href="#" id="pageNext_' + _divId + '">Next</a>';
        dg += '</li>';

        $('#ulPage_' + _divId).html(dg);


        if (_page == 1) {
            $('#pagePrevious_' + _divId).addClass('disabled');
            if (totalPage == 1) {
                $('#pageNext_' + _divId).addClass('disabled');
            } else {
                $('#pageNext_' + _divId).removeClass('disabled');
            }
        } else if (totalPage == _page) {
            $('#pagePrevious_' + _divId).removeClass('disabled');
            $('#pageNext_' + _divId).addClass('disabled');
        } else {
            $('#pagePrevious_' + _divId).removeClass('disabled');
            $('#pageNext_' + _divId).removeClass('disabled');
        }

        $('#pagePrevious_' + _divId).on('click', function () {
            if ($('#pagePrevious_' + _divId).hasClass('disabled')) {
                return;
            }

            $('#' + _divId).zhTable('changePage', 'pre');
        });

        $('#pageNext_' + _divId).on('click', function () {
            if ($('#pageNext_' + _divId).hasClass('disabled')) {
                return;
            }
            $('#' + _divId).zhTable('changePage', 'next');
        });

        $('a[name="btnChangePage"').on('click', function () {
            $('#' + _divId).zhTable('changePage', parseInt($(this).text()));
        })
    }

    function btnChangePage(selPage) {
        _page = selPage;
        btnQuery('');
    }



    function btnSort(elementId, _divId) {
        //▼= &#9660; 
        //▲= &#9650;

        $('span[name="_order_' + _divId + '"]').html('');

        var _order = $('#' + elementId).attr('order');
        if (_order == 'desc') {
            _order = elementId.replace('tbl_' + _divId + '_', '') + ' asc';
            $('#' + elementId).attr('order', 'asc');
            $('#order_' + elementId).html('&#9650;');
        } else {
            _order = elementId.replace('tbl_' + _divId + '_', '') + ' desc';
            $('#' + elementId).attr('order', 'desc');
            $('#order_' + elementId).html('&#9660;');
        }

        $('#' + _divId).zhTable('sort', _order);
    }
})(jQuery);