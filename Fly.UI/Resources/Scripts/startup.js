(function ($) {
    // 全局的Ajax处理
    // 请求前
    $(document).ajaxSend(function (event, jqxhr, settings, thrownError) {
        // alert('start send');
    });
    // 请求成功后
    $(document).ajaxSuccess(function (event, jqxhr, settings, data) {
        if (data.error) {
            var msg = data.message ? data.message : '出现错误，请稍后再试！';
            if (msg == 'No_Authenticated') {
                IBBAlert.Error('请先登录！');
                setTimeout(function () {
                    window.location.href = data.loginUrl;
                }, 3 * 1000);
            }
            else if (msg == 'No_Authorized') {
                IBBAlert.Error('对不起，您没有此操作的操作权限！');
            } else {
                IBBAlert.Error(msg);
            }
        }
    });
    // 请求失败后
    $(document).ajaxError(function (event, jqxhr, settings, thrownError) {
        IBBAlert.Error('出现错误，请稍后再试！');
    });

    // Bootstrap Modal 全局默认设置
    $.fn.modal.Constructor.DEFAULTS.backdrop = 'static'; // 显示半透明背景层，但是点击背景层不隐藏Modal
    $.fn.modal.Constructor.DEFAULTS.keyboard = true; // 按'Esc'键隐藏Modal
})(jQuery);

$(document).ready(function () {
    // Sidebar Menu
    $('#site-menu').metisMenu();

    var minCls = 'sidebar-min';
    var wrapper = $('#wrapper');
    var isMin = $.cookie('sidebarMin') == '1' ? true : false;
    if (isMin) {
        wrapper.addClass(minCls);
    }
    // min sidebar
    $('#site-menu a.sidebar-toggle').click(function () {
        if (wrapper.hasClass(minCls)) {
            wrapper.removeClass(minCls);
            $.cookie('sidebarMin', '0', { path: '/', 'expires': 30 });
            isMin = false;
        } else {
            wrapper.addClass(minCls);
            $.cookie('sidebarMin', '1', { path: '/', 'expires': 30 });
            isMin = true;
        }
    });

    $(window).bind('load resize', function () {
        var height = $(window).height() - 60;
        $('#page-content').css('min-height', height + 'px');

        // collapse sidebar menu when window width is not enough
        var width = (this.window.innerWidth > 0) ? this.window.innerWidth : this.screen.width;
        if (!isMin) {
            if (width < 775) {
                wrapper.addClass(minCls);
            } else {
                wrapper.removeClass(minCls);
            }
        }
    });
});
$(document).ready(function () {
    // breadcrumb scroll
    $(window).scroll(breadcrumbScroll);
    $(window).resize(breadcrumbScroll);

    $('[data-toggle="tooltip"]').tooltip();

    // render combobox
    $(".combobox").each(function (index, element) {
        var args = $(this).attr("data-config");
        var config = {};
        if ($.trim(args).length > 0) {
            try {
                config = eval("(" + args + ")");
                var x = $(this).magicSuggest(config);
                return;
            }
            catch (e) { }
        }
        url = $(this).attr("url");
        var ms = $(this).magicSuggest({
            data: url,
            maxSelection: 1,
            allowFreeEntries: false,
        });
        ms.setName($(this).attr("name"));
    });

    toastr.options.positionClass = 'toast-bottom-right';
    toastr.options.closeButton = true;
    toastr.options.timeOut = 5 * 1000;

    function breadcrumbScroll() {
        var h_scroll = $(document).scrollTop();
        if (h_scroll >= 20) {
            $('#page-content').addClass('scrolling');
        } else {
            $('#page-content').removeClass('scrolling');
        }
    }
});

$(function () {
    $('.ec-table').each(function () {
        var obj = $(this).find('.table-body')[0];
        //alert(obj.scrollHeight + "," + obj.clientHeight);
        //alert(obj.offsetHeight + "," + obj.clientHeight);
        var hasScroll = (obj.scrollHeight > obj.clientHeight + 1 || obj.offsetHeight > obj.clientHeight + 1);
        var td = $(this).find('.table-caption .scrollbar');
        var otd = td.prev();
        if (hasScroll) {
            td.show();
            otd.css("border-right", "1px");
        }
        else {
            td.hide();
            otd.css("border-right", "0px");
        }
    });
});

/**
 *ckeditor富文本框
 * @param id - 容器的Id，只支持id选择容器
 * @param category - 图片上传到阿里云的分类（文件夹）
 * @param height - 高度，可不传，默认500
 * @param width - 宽度，可不传，默认自适应
 	var brandEditor = new IBBEditor('ckeditor_brand','brand');
	brandEditor.SetData(内容);//设置富文本初始数据
	brandEditor.GetData();//获取数据
 */
function IBBEditor(id, category, height, width) {
    if ($('#' + id).length < 1)
        return;

    if (CKEDITOR.env.ie && CKEDITOR.env.version < 9) {
        CKEDITOR.tools.enableHtml5Elements(document);
    }

    var h = 500;
    if (height)
        h = height;

    var w = 'auto';
    if (width)
        w = width;

    CKEDITOR.replace(id, {
        height: h,
        width: w,
        filebrowserImageUploadUrl: '/commonservice/uploadimageforckeditor/?category=' + category
    });
    this.Instance = CKEDITOR.instances[id];
    this.GetData = function () { return this.Instance.getData() };
    this.SetData = function (content) {
        this.Instance.setData(content);
    };
}

// query panel
$(document).ready(function () {
    var queryPanel = $('.query-panel');
    var showFormBtn = queryPanel.find('.show-form');
    var queryForm = queryPanel.find('.query-form');
    var showMoreBtn = queryPanel.find('.show-more');
    var moreFields = queryPanel.find('.more-fields');

    queryForm.hide();
    moreFields.hide();

    showFormBtn.click(function () {
        if (queryPanel.hasClass('can-not-collapse')) {
            return;
        }
        queryForm.slideToggle();
    });

    showMoreBtn.click(function () {
        moreFields.slideToggle(function () {
            var icon = showMoreBtn.find('i.fa');
            if (icon.hasClass('fa-plus')) {
                icon.removeClass('fa-plus').addClass('fa-minus');
            } else {
                icon.removeClass('fa-minus').addClass('fa-plus');
            }
        });
    });
});

// grid panel
$(document).ready(function () {
    var _gridCheck = function (selector) {
        var gridPanel = $(selector);

        var allCheck = gridPanel.find('.grid-head-check');
        var singleCheck = gridPanel.find('.grid-row-check').not(':disabled');

        allCheck.each(function () {
            $(this).change(function () {
                var checked = $(this).prop('checked');
                allCheck.prop('checked', checked);
                singleCheck.each(function () {
                    if ($(this).is(':disabled') == false) {
                        $(this).prop('checked', checked);
                    }
                });
            });
        });

        singleCheck.each(function () {
            $(this).change(function () {
                var allChecked = true;
                singleCheck.each(function () {
                    if (!$(this).prop('checked')) {
                        allChecked = false;
                    }
                });
                allCheck.prop('checked', allChecked);
            });
        });

        return {
            getChecked: function () {
                var ids = [];
                singleCheck.each(function () {
                    if ($(this).prop('checked')) {
                        ids.push($(this).data('id'));
                    }
                });
                return ids;
            }
        };
    }

    window.IBBGridCheck = _gridCheck('.grid-panel');
    window.IBBGridCheck2 = _gridCheck;

    var _specialGrid = function (gridWrapObj, gridHeadingSelector, itemSelector, itemHeadingSelector, itemBodySelector) {
        var timeOutFun = null;
        var _showOrHideBody = function (fa, isSingle) {
            var context = isSingle ? fa.closest(itemSelector) : gridWrapObj;

            if (fa.hasClass('fa-minus-circle')) {
                context.find(itemBodySelector).hide();
                context.find('.body-toggle').each(function () {
                    $(this).addClass('fa-plus-circle').removeClass('fa-minus-circle');
                });
            } else {
                context.find(itemBodySelector).show();
                context.find('.body-toggle').each(function () {
                    $(this).addClass('fa-minus-circle').removeClass('fa-plus-circle');
                });
            }
        };
        var _checkboxToggle = function (fa, isSingle) {
            var context = isSingle ? fa.closest(itemSelector) : gridWrapObj;

            clearTimeout(timeOutFun);
            timeOutFun = setTimeout(function () {
                if (fa.hasClass('fa-check-square-o')) {
                    context.find('.checkbox-toggle').each(function () {
                        $(this).addClass('fa-square-o').removeClass('fa-check-square-o');
                    });
                } else {
                    context.find('.checkbox-toggle').each(function () {
                        $(this).addClass('fa-check-square-o').removeClass('fa-square-o');
                    });
                }
            }, 300);
        };

        gridWrapObj.find(gridHeadingSelector + ', ' + itemHeadingSelector).dblclick(function (e) {
            clearTimeout(timeOutFun);

            var heading = $(this);
            var isSingle = heading.hasClass(itemHeadingSelector.replace('.', ''));

            var target = $(e.target);
            var parent = target.parent();
            var tagName = target.get(0).tagName.toLowerCase();
            var parentName = parent.get(0).tagName.toLowerCase();

            if (tagName == 'a' || parentName == 'a'
                || tagName == 'button' || parentName == 'button') {
                return true;
            } else if (target.hasClass('body-toggle')) {
                _showOrHideBody(target, isSingle);
                return false;
            } else if (target.hasClass('checkbox-toggle')) {
                _checkboxToggle(target, isSingle);
                return false;
            } else {
                _showOrHideBody(heading.find('.body-toggle'), isSingle);
                return true;
            }
        });

        gridWrapObj.find(gridHeadingSelector + ', ' + itemHeadingSelector).click(function (e) {
            clearTimeout(timeOutFun);

            var heading = $(this);
            var isSingle = heading.hasClass(itemHeadingSelector.replace('.', ''));

            var target = $(e.target);
            var parent = target.parent();
            var tagName = target.get(0).tagName.toLowerCase();
            var parentName = parent.get(0).tagName.toLowerCase();

            if (tagName == 'a' || parentName == 'a'
                || tagName == 'button' || parentName == 'button') {
                return true;
            } else if (target.hasClass('body-toggle')) {
                _showOrHideBody(target, isSingle);
                return false;
            } else if (target.hasClass('checkbox-toggle')) {
                _checkboxToggle(target, isSingle);
                return false;
            } else {
                _checkboxToggle(heading.find('.checkbox-toggle'), isSingle);
                return true;
            }
        });

        if (gridWrapObj.hasClass('expanded')) {
            var target = gridWrapObj.find(gridHeadingSelector).find('.body-toggle');
            _showOrHideBody(target, false);
        }
    }

    $('.special-grid-panel').each(function () {
        _specialGrid($(this), '.grid-heading', '.panel-item', '.item-heading', '.item-body');
    });

    function _specialGridCheck(gridSelector) {
        return {
            getChecked: function () {
                var ids = [];
                $(gridSelector).find('.checkbox-toggle.fa-check-square-o').each(function () {
                    var id = $(this).data('id');
                    if (id) {
                        ids.push(id);
                    }
                })
                return ids;
            }
        };
    }
    window.InitSpecialGrid = _specialGrid;
    window.IBBSpecialGridCheck = _specialGridCheck('.special-grid-panel');
});

// grid actions
/**
 * popup a modal to show a confirm remove modal
 * @param removeUrl - the url to ajax post when clicked Remove button
 * @param onRemoved - function to be called when remove process is completed
 * @param identity - parameter passed to onRemoved function, must be string
 */
function GridRemoveData(removeUrl, onRemoved, identity) {
    IBBAlert.Confirm('一旦删除将不能恢复，确认删除？', function (yes) {
        if (yes) {
            $.ajax({
                type: 'POST',
                dataType: 'json',
                url: removeUrl,
                data: {},
                success: function (data) {
                    if (!data.error) {
                        var msg = data.data.message;
                        if (!msg) {
                            msg = "操作成功！";
                        }
                        toastr.success(msg);
                        if (typeof onRemoved === 'function') {
                            onRemoved(identity);
                        }
                    } else {
                        toastr.error(data.message);
                    }
                }
            });
        }
    });
}

// 给Query Panel 自动赋值
$(document).ready(function () {
    function AssignmentQueryParms() {
        var qpanel = $('.query-panel');
        qpanel.find('.form-group input').each(function () {
            var name = $(this).attr('name');
            if (name) {
                $(this).attr('name', name.toLowerCase());
            }
        });
        qpanel.find('.form-group select').each(function () {
            var name = $(this).attr('name');
            if (name) {
                $(this).attr('name', name.toLowerCase());
            }
        });

        var query = window.location.search.substring(1); // 获取查询串(不带?)
        var pairs = query.split('&'); // 在&处断开
        for (var i = 0; i < pairs.length; i++) {
            var pos = pairs[i].indexOf('='); // 查找name=value
            if (pos == -1) {
                continue; // 如果没有找到就跳过
            }

            var name = pairs[i].substring(0, pos); // 提取name
            var value = pairs[i].substring(pos + 1); // 提取value
            value = decodeURI(value);
            if (!name || !value) {
                continue;
            }
            name = name.toLowerCase();

            if (qpanel.find('.form-group input:radio[name="' + name + '"]').length > 0) { // radio
                qpanel.find('.form-group input:radio[name="' + name + '"]').each(function () {
                    var vv = $(this).val();
                    if (vv && vv.toLowerCase() == value.toLowerCase()) {
                        $(this).prop('checked', true);
                    }
                });
            } else if (qpanel.find('.form-group input:checkbox[name="' + name + '"]').length > 0) { // checkbox
                var splitedVals = [];
                var strs = value.split(',');
                for (j = 0; j < strs.length; j++) {
                    splitedVals.push(strs[j].toLowerCase());
                }
                qpanel.find('.form-group input:checkbox[name="' + name + '"]').each(function () {
                    var vv = $(this).val();
                    if (vv && $.inArray(vv.toLowerCase(), splitedVals) > -1) {
                        $(this).prop('checked', true);
                    }
                });
            } else if (qpanel.find('.form-group input:text[name="' + name + '"]').length > 0) { // input
                qpanel.find('.form-group input:text[name="' + name + '"]').each(function () {
                    if ($(this).hasClass('dtp-input') == false) {
                        $(this).val(value);
                    }
                });
            } else if (qpanel.find('.form-group select[name="' + name + '"]').length > 0) { // select/bootstrap select
                qpanel.find('.form-group select[name="' + name + '"]').each(function () {
                    $(this).val(value);
                    $(this).selectpicker('render');
                });
            }
        }
    }
    AssignmentQueryParms();
});

/**
 * 通用的启用/禁用/删除/...操作
 * #iptBulkActionUrl
 */
function BulkStatusAction(btn, ids, act, callback) {
    if (!ids || ids.length <= 0) {
        IBBAlert.Error('请先选择需要操作的项！');
        return;
    }
    var len = ids.length;
    IBBAlert.Confirm('确认要对选中的' + len + '行数据进行该操作？', function (yes) {
        if (yes) {
            _doBulkAction(btn, ids, act, callback);
        }
    });

    function _doBulkAction(btn, ids, act, callback) {
        btn.button('loading');
        $.ajax({
            type: 'POST',
            dataType: 'json',
            url: $('#iptBulkActionUrl').val(),
            data: {
                ids: ids.join(','),
                act: act
            },
            success: function (data) {
                btn.button('reset');
                if (!data.error) {
                    toastr.success('', '操作成功！');
                    if (typeof callback == 'function') {
                        callback(data.data);
                    } else {
                        window.location.reload();
                    }
                }
            },
            error: function () {
                btn.button('reset');
            }
        });
    }
}
/**
 * 通用简单的查询页面的启用/禁用/删除操作
 * .common-query-page
 * #btnBulkEnable/#btnBulkDisable/#btnBulkDelete
 * .btn-enable/.btn-disable/.btn-delete
 * tr data-id="xxx"
 */
$(document).ready(function () {
    $('.common-query-page').each(function () {
        var page = $(this);

        // 批量启用
        $('#btnBulkEnable').click(function () {
            var ids = IBBGridCheck.getChecked();
            BulkStatusAction($(this), ids, 'Enable');
        });
        // 批量禁用
        $('#btnBulkDisable').click(function () {
            var ids = IBBGridCheck.getChecked();
            BulkStatusAction($(this), ids, 'Disable');
        });
        // 批量删除
        $('#btnBulkDelete').click(function () {
            var ids = IBBGridCheck.getChecked();
            BulkStatusAction($(this), ids, 'Delete');
        });
        // 单个启用
        page.find('.grid-panel tr td .btn-enable').click(function () {
            var btn = $(this);
            var ids = [btn.closest('tr').data('id')];
            BulkStatusAction(btn, ids, 'Enable');
        });
        // 单个禁用
        page.find('.grid-panel tr td .btn-disable').click(function () {
            var btn = $(this);
            var ids = [btn.closest('tr').data('id')];
            BulkStatusAction(btn, ids, 'Disable');
        });
        // 单个删除
        page.find('.grid-panel tr td .btn-delete').click(function () {
            var btn = $(this);
            var ids = [btn.closest('tr').data('id')];
            BulkStatusAction(btn, ids, 'Delete');
        });
    });
});

/**
 * 图片上传到阿里云
 * @param container{jquery 对象} 图片放置的容器，如：'.container'
 * @param folder {字符串} 图片上传阿里云的文件夹
 * @param fileLimitedNumber {数字} 需要上传的图片个数限制，可不传，表示图片没有个数没有限制
 * 使用例子：
	var IBBUploader = new IBBUploader($('.logo-container'), 'product');
	IBBUploader.GetData();//获取上传后的图片key数组
	IBBUploader.Reset();//重置上传控件，如：保存新增的时候
	//如果是编辑页面，可以赋予初始值在container标签的data-images属性上,多个图片以,间隔，如<div class="logo-container" data="1.jpg,2.jpg" ></div>
  **/
function IBBUploader(container, folder, fileLimitedNumber, customInitFunc, itemBuilderFunc, uploadSuccessCallback, deleteCallback) {
    var _container = container;
    var uploaderWrap = null; // .oss-uploader
    var photoList = null; // .photos
    var uploader = null;
    var fileStorageKey;
    if ($(_container).data('filestoragekey')) {
        fileStorageKey = $(_container).data('filestoragekey');
    }

    var fileLimited = fileLimitedNumber || 10000;
    if (fileLimited < 1) {
        fileLimited = 1;
    }

    var getHtmlUrl = '/CommonService/OSSGetUploaderHtml/'.toLowerCase();
    var uploadUrl = '/CommonService/OSSUploadImage/'.toLowerCase();
    var deleteUrl = '/CommonService/OSSDeleteImage/'.toLowerCase();

    this.Reset = function () {
        photoList.empty();
        uploader.reset();
        _hideOrShowUploadBtn();
    }
    this.GetData = function () {
        var list = [];
        photoList.find('.photo').each(function () {
            list.push($(this).data('key'));
        });
        return list;
    }

    var _hideOrShowUploadBtn = function () {
        var picker = uploaderWrap.find('.image-picker');
        var count = photoList.find('.photo').length;
        if (count >= fileLimited) {
            picker.hide();
        } else {
            picker.show();
        }
    }
    var _showErrorMsg = function (msg) {
        var errorMsg = uploaderWrap.find('.error-msg');
        errorMsg.empty().append('<div class="msg">' + msg + '</div>').show();
        setTimeout(function () {
            errorMsg.fadeOut(2000, function () {
                errorMsg.empty();
            });
        }, 2000);
    }
    var _delete = function (item) {
        var key = item.data('key');
        if (!key) { return; }

        IBBAlert.Confirm('确认删除图片？', function (yes) {
            if (yes) {
                if (typeof deleteCallback == 'function') {
                    deleteCallback(item);
                } else {
                    item.remove();
                    var id = item.attr('id');
                    if (id) {
                        uploader.removeFile(id, true);
                    }
                    _hideOrShowUploadBtn();
                }
            }
        });

        // $.ajax({
        // 	type: 'POST',
        // 	dataType: 'Json',
        // 	data: { key: key },
        // 	url: deleteUrl,
        // 	success: function() {
        // 		var id = item.attr('id');
        // 		if (id) {
        // 			uploader.removeFile(id, true);
        // 		}

        // 		if (typeof deleteCallback == 'function') {
        // 			deleteCallback(item);
        // 		} else {
        // 			item.remove();
        // 			_hideOrShowUploadBtn();
        // 		}
        // 	},
        // 	error: function() {
        // 		_showErrorMsg('删除图片失败！');
        // 	}
        // });
    }
    var _init = function () {
        var opts = {
            pick: {
                id: uploaderWrap.find('.image-picker'),
                innerHTML: '点击选择照片',
                multiple: fileLimited > 1
            },
            formData: { folder: folder, fileStorageKey: fileStorageKey },
            server: uploadUrl,
            compress: false,
            auto: true,
            duplicate: true,
            fileVal: 'file',
            accept: {
                title: 'Images',
                extensions: 'jpg,jpeg,png',
                mimeTypes: 'image/*'
            },
            disableGlobalDnd: true,
            fileSingleSizeLimit: 10 * 1024 * 1024,  // 10 M
            fileNumLimit: fileLimited
        };

        uploader = WebUploader.create(opts);
        _hideOrShowUploadBtn();

        _container.on('click', '.btn-remove', function () {
            var item = $(this).closest('.photo');
            _delete(item);
        });
        // 添加图片时
        uploader.on('fileQueued', function (file) {
            var tpl = '';
            if (typeof itemBuilderFunc == 'function') {
                tpl = itemBuilderFunc(file);
            } else {
                tpl = [
                    '<div class="photo ' + file.id + '" id="' + file.id + '" uploaded="0">',
                    '<div class="image">',
                    '<div class="progress"><div class="bar"></div></div>',
                    '</div>',
                    '<div class="top"><a class="btn-remove" href="javascript:void(0);"><i class="fa fa-trash"></i></a></div>',
                    '</div>'
                ].join('');
            }

            var item = $(tpl);
            photoList.append(item);

            _hideOrShowUploadBtn();
        });
        // 图片上传时
        uploader.on('uploadProgress', function (file, percentage) {
            var item = photoList.find('.' + file.id);
            var bar = item.find('.progress .bar');
            bar.css('width', percentage * 100 + '%');
        });
        // 图片上传成功
        uploader.on('uploadSuccess', function (file, response) {
            var item = photoList.find('.' + file.id);
            item.find('.progress').remove();

            if (!response.error) {
                item.find('.image').append('<img src="' + response.data.thumbnailUrl + '" width="100" height="100" />');
                item.data('key', response.data.key);
                item.data('uploaded', '1');
            } else {
                _showErrorMsg('上传图片错误');
            }
            _hideOrShowUploadBtn();

            if (typeof uploadSuccessCallback == 'function') {
                uploadSuccessCallback(item, response);
            }
        });
        // 出错
        uploader.on('error', function (code) {
            var text = '出错了！';
            if (code == 'Q_TYPE_DENIED') {
                text = '只能上传图片！';
            } else if (code == 'Q_EXCEED_NUM_LIMIT') {
                text = '抱歉，最多只能上传' + fileLimited + '张图片！';
            } else if (code == 'Q_EXCEED_SIZE_LIMIT') {
                text = '抱歉，上传的图片太大了！';
            } else if (code == 'F_EXCEED_SIZE') {
                text = '抱歉，上传的图片太大了！';
            } else {
                text = code;
            }
            _showErrorMsg(text);
            _hideOrShowUploadBtn();
        });
    }

    var preload = {};
    if (!customInitFunc) {
        preload.preload = _container.data('images');
    }

    _container.html('<span style="color:#999;">图片上传控件加载中...</span>');
    $.ajax({
        type: 'POST',
        dataType: 'Json',
        data: preload,
        url: getHtmlUrl,
        success: function (data) {
            if (!data.error) {
                _container.html(data.data.html);
                uploaderWrap = _container.find('.oss-uploader');
                photoList = uploaderWrap.find('.photos');

                if (typeof customInitFunc == 'function') {
                    customInitFunc(photoList);
                }
                _init();
            } else {
                _container.html('<span style="color:red">加载图片上传控件出错</span>');
            }
        }
    })
}

function IBBCommonFileUploader(options) {
    var defaults = {
        container: null,
        fileStorageKey: '',
        folder: '',
        fileLimited: 10000,
        itemBuilderFunc: null,
        uploadSuccessCallback: null,
        deleteCallback: null
    };
    var settings = $.extend({}, defaults, options);

    if (!settings.container) { return; }
    if (settings.fileLimited < 1) { settings.fileLimited = 1; }

    var filePicker = settings.container.find('.file-picker');
    var fileList = settings.container.find('.file-list');

    var opts = {
        pick: {
            id: filePicker,
            innerHTML: '选择文件',
            multiple: settings.fileLimited > 1
        },
        formData: { folder: settings.folder, fileStorageKey: settings.fileStorageKey },
        server: '/CommonService/OSSUploadFile/'.toLowerCase(),
        compress: false,
        auto: true,
        duplicate: true,
        fileVal: 'file',
        disableGlobalDnd: true,
        fileSingleSizeLimit: 50 * 1024 * 1024,  // 20 M
        fileNumLimit: settings.fileLimited
    };
    var uploader = WebUploader.create(opts);
    filePicker.addClass('btn btn-primary btn-sm');
    _hideOrShowUploadBtn();

    settings.container.on('click', '.btn-remove', function () {
        var item = $(this).closest('.item');
        _delete(item);
    });
    // 添加时
    uploader.on('fileQueued', function (file) {
        var tpl = '';
        if (typeof settings.itemBuilderFunc == 'function') {
            tpl = settings.itemBuilderFunc(file);
        } else {
            tpl = [
                '<div class="item ' + file.id + '" id="' + file.id + '" data-uploaded="0">',
                '<div class="file-name">',
                '<div class="progress"><div class="progress-bar"></div></div>',
                '</div>',
                '<a class="btn-remove" href="javascript:;"><i class="fa fa-times"></i></a>',
                '<div class="clearfix"></div>',
                '</div>'
            ].join('');
        }

        var item = $(tpl);
        fileList.append(item);

        _hideOrShowUploadBtn();
    });
    // 上传时
    uploader.on('uploadProgress', function (file, percentage) {
        var item = fileList.find('.' + file.id);
        var bar = item.find('.progress .progress-bar');
        bar.css('width', percentage * 100 + '%');
    });
    // 上传成功
    uploader.on('uploadSuccess', function (file, response) {
        var item = fileList.find('.' + file.id);
        item.find('.progress').remove();

        if (!response.error) {
            item.find('.file-name').append('<a href="' + response.data.originUrl + '" target="_blank"><i class="fa fa-file-text"></i> ' + response.data.name + '</a>');
            item.data('key', response.data.key);
            item.data('name', response.data.name);
            item.data('uploaded', '1');
        } else {
            _showErrorMsg('上传错误');
        }
        _hideOrShowUploadBtn();

        if (typeof settings.uploadSuccessCallback == 'function') {
            settings.uploadSuccessCallback(item);
        }
    });
    // 出错
    uploader.on('error', function (code) {
        var text = '出错了！';
        if (code == 'Q_TYPE_DENIED') {
            text = '只能上传图片！';
        } else if (code == 'Q_EXCEED_NUM_LIMIT') {
            text = '抱歉，最多只能上传' + fileLimited + '张图片！';
        } else if (code == 'Q_EXCEED_SIZE_LIMIT') {
            text = '抱歉，上传的图片太大了！';
        } else if (code == 'F_EXCEED_SIZE') {
            text = '抱歉，上传的图片太大了！';
        } else {
            text = code;
        }
        _showErrorMsg(text);
        _hideOrShowUploadBtn();
    });

    return {
        GetData: function () {
            var list = [];
            fileList.find('.item').each(function () {
                list.push($(this).data('key'));
            });
            return list;
        }
    };
    function _hideOrShowUploadBtn() {
        var count = fileList.find('.item').length;
        if (count >= settings.fileLimited) {
            filePicker.hide();
        } else {
            filePicker.show();
        }
    }
    function _showErrorMsg(msg) {
        toastr.error(msg);
    }
    function _delete(item) {
        var key = item.data('key');
        if (!key) { return; }

        if (typeof settings.deleteCallback == 'function') {
            settings.deleteCallback(item);
        } else {
            item.remove();
            _hideOrShowUploadBtn();
        }
    }
}

/*
 * 级联选择控件
 * @param container{字符串} 控件的容器，如：'.container'
 * @param url{字符串} 数据加载的url
 * @param method{字符串} HTTP请求方式，默认为POST
 * @param parameterName{字符串} 数据加载的参数名，默认为parentId
 * @param list{字符串} 返回数据的列表名，默认为list
 * @param value{字符串} 返回数据的列表项的值名，默认为Value
 * @param text{字符串} 返回数据的列表项的文本名，默认为Text
 * @param search{布尔} 选择是否可以搜索，默认为false
 * @param selectValue{字符串} 赋值
 * @param loadParentUrl{字符串} 加载父类的url
 * @param loadParentParameterName{字符串} 加载父类的时请求的参数名
 * @param backParentValueParameterName{字符串} 返回往上一级父类的值的参数名
 * @param notMustLeaf{布尔} 是否不需要选到子节点
 * @method changeCallBack{function} 选择改变时的callback方法
 * @method GetData() 获取最终的值
 * @method Reset() 重置选择控件
*/
function IBBCascade(container, url, method, parameterName, list, value, text, search, changeCallBack, selectValue, loadParentUrl, loadParentParameterName, backParentValueParameterName, notMustLeaf) {
    this.CurrentFloor = -1;
    this.Container = container;
    this.GetData = function () {
        var value = $(container + ' select.select-control').last().val();
        if (!value && notMustLeaf) {
            var selectLength = $(container + ' select.select-control').length;
            if (selectLength > 1) {
                value = $(container + ' select.select-control').eq(selectLength - 2).val();
            }
        }
        return value;
    }
    this.Reset = function () {
        for (i = 1; i <= me.CurrentFloor; i++) {
            $(me.Container + ' .floor' + i).unbind('change');
            $(me.Container + ' .floor' + i).selectpicker('destroy');
        }
        $(me.Container + ' .selectpicker.floor0').selectpicker('val', '');
        if (changeCallBack)
            changeCallBack(this.GetData());
    }


    me = this;
    if (!method)
        method = 'POST';
    if (!parameterName)
        parameterName = 'parentId';
    if (!list)
        list = 'list';
    if (!value)
        value = 'Value';
    if (!text)
        text = 'Text';
    if (!search)
        search = false;

    function LoadParent(key, floor, needLoadChild) {
        var prefloor = null;
        $.ajax({
            type: method,
            dataType: 'JSON',
            url: loadParentUrl,
            data: loadParentParameterName + '=' + key,
            success: function (r) {
                if (!floor)
                    floor = 0;

                if (!r.data || !r.data[list] || r.data[list].length == 0) {
                    $(me.Container + ' select.select-control').each(function (e) {
                        var floor = $(this).attr('data-floor');
                        floor = -floor;
                        floor = me.CurrentFloor - floor;
                        $(this).addClass('floor' + floor);
                        $(this).attr('data-floor', floor)
                    })
                    for (j = 0; j <= me.CurrentFloor; j++) {
                        var currFloor = $(me.Container + ' .selectpicker.select-control.floor' + j);

                        $(currFloor).selectpicker('refresh');

                        var selectValue = $(currFloor).attr('select-value');

                        $(currFloor).selectpicker('val', selectValue);

                        $(me.Container + ' .bootstrap-select.floor' + j).css('margin-bottom', '10px');


                        $(currFloor).bind('change', function (e) {
                            var currFloorIndex = parseInt($(this).attr('data-floor'));
                            for (i = currFloorIndex + 1; i <= me.CurrentFloor; i++) {
                                $(me.Container + ' .floor' + i).unbind('change');
                                $(me.Container + ' .floor' + i).selectpicker('destroy');
                            }

                            if (!$(this).val()) {
                                if (changeCallBack) {
                                    changeCallBack(me.GetData());
                                }
                                return;
                            }
                            LoadChild($(this).val(), currFloorIndex + 1);
                        });
                    }
                    if (needLoadChild) {
                        LoadChild(selectValue, me.CurrentFloor + 1)
                    }
                    return;
                }

                var html = '<select class="selectpicker  form-control select-control floortemp' + floor + '" data-floor="' + (-floor) + '" data-live-search="' + search + '">'

                if (r.data[list][0] && r.data[list][0][value] != null && r.data[list][0][value] != '') {
                    if (!notMustLeaf)
                        html += '<option value="">请选择</option>';
                    else
                        html += '<option value="">全部的</option>';
                }

                for (i = 0; i < r.data.list.length; i++) {
                    var item = r.data[list][i];
                    html += '<option value="' + item[value] + '">' + item[text] + '</option>';
                }
                html += '</select>';
                $(me.Container).prepend(html);

                me.CurrentFloor = floor;


                $(me.Container + ' .floortemp' + floor).attr('select-value', key);

                var parentId = r.data[backParentValueParameterName];

                LoadParent(parentId, floor + 1, needLoadChild);
            }
        });
    }

    if (selectValue) {
        if (notMustLeaf) {
            LoadParent(selectValue, 0, true);
        }
        else
            LoadParent(selectValue, 0);
    }
    else
        LoadChild();

    function LoadChild(key, floor) {
        $.ajax({
            type: method,
            dataType: 'JSON',
            url: url,
            data: parameterName + '=' + key,
            success: function (r) {
                if (!floor)
                    floor = 0;

                if (r.data[list][0] && !r.data[list] || r.data[list].length == 0 && !notMustLeaf) {
                    if (changeCallBack)
                        changeCallBack(me.GetData())
                    return;
                } else if (notMustLeaf) {
                    if (changeCallBack)
                        changeCallBack(me.GetData())
                }

                var html = '<select class="selectpicker  form-control select-control floor' + floor + '" data-live-search="' + search + '">'
                if (r.data[list][0] && r.data[list][0][value] != null && r.data[list][0][value] != '') {
                    if (!notMustLeaf)
                        html += '<option value="">请选择</option>';
                    else
                        html += '<option value="">全部的</option>';
                }
                else {
                    return;
                }
                for (i = 0; i < r.data.list.length; i++) {
                    var item = r.data[list][i];
                    html += '<option value="' + item[value] + '">' + item[text] + '</option>';
                }
                html += '</select>';
                $(html).appendTo($(me.Container));
                $(me.Container + ' .floor' + floor).selectpicker('refresh');
                $(me.Container + ' .bootstrap-select.floor' + floor).css('margin-bottom', '10px')

                me.CurrentFloor = floor;

                if (changeCallBack) {
                    changeCallBack(me.GetData());
                }

                $(me.Container + ' .floor' + floor).bind('change', function (e) {
                    for (i = floor + 1; i <= me.CurrentFloor; i++) {
                        $(me.Container + ' .floor' + i).unbind('change');
                        $(me.Container + ' .floor' + i).selectpicker('destroy');
                    }

                    me.CurrentFloor = floor;

                    if (!$(this).val()) {
                        if (changeCallBack) {
                            changeCallBack(me.GetData());
                        }
                        return;
                    }
                    LoadChild($(this).val(), floor + 1);
                });
            }
        });
    }
}

function AreaSelector(controlHolder, valueHolder, selectedAreaId, disabled, provinceSelectText, citySelectText, districtSelectText) {
    _loadParent(selectedAreaId, null);
    _loadChild(selectedAreaId, null);

    controlHolder.on('change', 'select', function () {
        var select = $(this);
        var selectedVal = select.val();
        var obj = select.closest('.bootstrap-select');

        obj.nextAll('.bootstrap-select').remove();
        if (selectedVal) {
            _loadChild(selectedVal, obj);
        }

        var currAreaId = '';
        controlHolder.find('select').each(function () {
            var tmp = $(this).val();
            if (tmp) {
                currAreaId = tmp;
            }
        });
        valueHolder.val(currAreaId);
    });

    return {
        Reset: function (areaId) {
            if (!areaId) {
                areaId = '';
            }
            controlHolder.empty();
            valueHolder.val(areaId);
            _loadParent(areaId, null);
            _loadChild(areaId, null);
        },
        GetData: function () {
            return valueHolder.val();
        }
    };

    function _loadParent(areaId, currObj) {
        $.ajax({
            type: 'POST',
            dataType: 'json',
            url: '/CommonService/LoadParentAreas/'.toLowerCase(),
            data: { areaId: areaId },
            success: function (data) {
                if (!data.error) {
                    var list = data.data.list;
                    var parentId = data.data.parentId;
                    var cls = GenerateUniqueId();

                    var html = _buildSelect(list, areaId, cls);

                    if (currObj) {
                        currObj.prevAll('.bootstrap-select').remove();
                        currObj.before(html);
                    } else {
                        controlHolder.prepend(html);
                    }
                    controlHolder.find('select').selectpicker('render');

                    if (parentId) {
                        var currObj2 = controlHolder.find('.bootstrap-select.' + cls);
                        _loadParent(parentId, currObj2);
                    }
                }
            }
        });
    }
    function _loadChild(areaId, currObj) {
        $.ajax({
            type: 'POST',
            dataType: 'json',
            url: '/CommonService/LoadChildAreas/'.toLowerCase(),
            data: { areaId: areaId, provinceSelectText: provinceSelectText, citySelectText: citySelectText, districtSelectText: districtSelectText },
            success: function (data) {
                if (!data.error) {
                    var list = data.data.list;
                    var cls = GenerateUniqueId();

                    var html = _buildSelect(list, '', cls);

                    if (currObj) {
                        currObj.nextAll('.bootstrap-select').remove();
                        currObj.after(html);
                    } else {
                        controlHolder.append(html);
                    }
                    controlHolder.find('select').selectpicker('render');
                }
            }
        });
    }
    function _buildSelect(list, value, cls) {
        if (!list || list.length <= 0) {
            return '';
        }

        var selectDisabled = disabled ? 'disabled' : '';

        var html = '<select class="' + cls + '" data-live-search="1" data-size="10" ' + selectDisabled + '>';
        for (var i = 0; i < list.length; i++) {
            var item = list[i];
            var optSelected = item.Value == value ? 'selected' : '';
            var option = '<option value="' + item.Value + '" ' + optSelected + '>' + item.Text + '</option>';
            html += option;
        };
        html += '</select>';
        return html;
    }
}

function PostCategorySelector(options) {
    var controlHolder = options.controlHolder;
    var valueHolder = options.valueHolder;
    var selectedCategoryId = options.selectedCategoryId;
    var disabled = options.disabled;
    var emptyText = options.emptyText;
    var excluded = options.excluded;

    if (Array.isArray(excluded) == false) {
        excluded = [];
    }

    _loadParent(selectedCategoryId, null);
    _loadChild(selectedCategoryId, null);

    controlHolder.on('change', 'select', function () {
        var select = $(this);
        var selectedVal = select.val();
        var obj = select.closest('.bootstrap-select');

        obj.nextAll('.bootstrap-select').remove();
        if (selectedVal) {
            _loadChild(selectedVal, obj);
        }

        var currAreaId = '';
        controlHolder.find('select').each(function () {
            var tmp = $(this).val();
            if (tmp) {
                currAreaId = tmp;
            }
        });
        valueHolder.val(currAreaId);
    });

    return {
        Reset: function (categoryId) {
            if (!categoryId) {
                categoryId = '';
            }
            controlHolder.empty();
            valueHolder.val(categoryId);
            _loadParent(categoryId, null);
            _loadChild(categoryId, null);
        },
        GetData: function () {
            return valueHolder.val();
        }
    };

    function _loadParent(categoryId, currObj) {
        $.ajax({
            type: 'POST',
            dataType: 'json',
            url: '/Post/LoadParentCategory/'.toLowerCase(),
            data: { categoryId: categoryId, emptyText: emptyText },
            success: function (data) {
                if (!data.error && data.data) {
                    var list = data.data.list;
                    var parentId = data.data.parentId;
                    var cls = GenerateUniqueId();

                    var html = _buildSelect(list, categoryId, cls);

                    if (currObj) {
                        currObj.prevAll('.bootstrap-select').remove();
                        currObj.before(html);
                    } else {
                        controlHolder.prepend(html);
                    }
                    controlHolder.find('select').selectpicker('render');

                    if (parentId) {
                        var currObj2 = controlHolder.find('.bootstrap-select.' + cls);
                        _loadParent(parentId, currObj2);
                    }
                }
            }
        });
    }
    function _loadChild(categoryId, currObj) {
        $.ajax({
            type: 'POST',
            dataType: 'json',
            url: '/Post/LoadChildCategory/'.toLowerCase(),
            data: { categoryId: categoryId, emptyText: emptyText },
            success: function (data) {
                if (!data.error) {
                    var list = data.data.list;
                    var cls = GenerateUniqueId();

                    var html = _buildSelect(list, '', cls);

                    if (currObj) {
                        currObj.nextAll('.bootstrap-select').remove();
                        currObj.after(html);
                    } else {
                        controlHolder.append(html);
                    }
                    controlHolder.find('select').selectpicker('render');
                }
            }
        });
    }
    function _buildSelect(list, value, cls) {
        if (!list || list.length <= 0) {
            return '';
        }

        var selectDisabled = disabled ? 'disabled' : '';

        var html = '<select class="' + cls + '" ' + selectDisabled + '>';
        for (var i = 0; i < list.length; i++) {
            var item = list[i];
            var optSelected = item.Value == value ? 'selected' : '';
            var optDisabled = $.inArray(item.Value, excluded) > -1 ? 'disabled' : '';
            var option = '<option value="' + item.Value + '" ' + optSelected + ' ' + optDisabled + '>' + item.Text + '</option>';
            html += option;
        };
        html += '</select>';
        return html;
    }
}
function FrontCategorySelector(options) {
    var controlHolder = options.controlHolder;
    var valueHolder = options.valueHolder;
    var selectedCategoryId = options.selectedCategoryId;
    var disabled = options.disabled;
    var emptyText = options.emptyText;
    var excluded = options.excluded;

    if (Array.isArray(excluded) == false) {
        excluded = [];
    }

    _loadParent(selectedCategoryId, null);
    _loadChild(selectedCategoryId, null);

    controlHolder.on('change', 'select', function () {
        var select = $(this);
        var selectedVal = select.val();
        var obj = select.closest('.bootstrap-select');

        obj.nextAll('.bootstrap-select').remove();
        if (selectedVal) {
            _loadChild(selectedVal, obj);
        }

        var currAreaId = '';
        controlHolder.find('select').each(function () {
            var tmp = $(this).val();
            if (tmp) {
                currAreaId = tmp;
            }
        });
        valueHolder.val(currAreaId);
    });

    return {
        Reset: function (categoryId) {
            if (!categoryId) {
                categoryId = '';
            }
            controlHolder.empty();
            valueHolder.val(categoryId);
            _loadParent(categoryId, null);
            _loadChild(categoryId, null);
        },
        GetData: function () {
            return valueHolder.val();
        }
    };

    function _loadParent(categoryId, currObj) {
        $.ajax({
            type: 'POST',
            dataType: 'json',
            url: '/FrontCategory/LoadParentCategory/'.toLowerCase(),
            data: { categoryId: categoryId, emptyText: emptyText },
            success: function (data) {
                if (!data.error && data.data) {
                    var list = data.data.list;
                    var parentId = data.data.parentId;
                    var cls = GenerateUniqueId();

                    var html = _buildSelect(list, categoryId, cls);

                    if (currObj) {
                        currObj.prevAll('.bootstrap-select').remove();
                        currObj.before(html);
                    } else {
                        controlHolder.prepend(html);
                    }
                    controlHolder.find('select').selectpicker('render');

                    if (parentId) {
                        var currObj2 = controlHolder.find('.bootstrap-select.' + cls);
                        _loadParent(parentId, currObj2);
                    }
                }
            }
        });
    }
    function _loadChild(categoryId, currObj) {
        $.ajax({
            type: 'POST',
            dataType: 'json',
            url: '/FrontCategory/LoadChildCategory/'.toLowerCase(),
            data: { categoryId: categoryId, emptyText: emptyText },
            success: function (data) {
                if (!data.error) {
                    var list = data.data.list;
                    var cls = GenerateUniqueId();

                    var html = _buildSelect(list, '', cls);

                    if (currObj) {
                        currObj.nextAll('.bootstrap-select').remove();
                        currObj.after(html);
                    } else {
                        controlHolder.append(html);
                    }
                    controlHolder.find('select').selectpicker('render');
                }
            }
        });
    }
    function _buildSelect(list, value, cls) {
        if (!list || list.length <= 0) {
            return '';
        }

        var selectDisabled = disabled ? 'disabled' : '';

        var html = '<select class="' + cls + '" ' + selectDisabled + '>';
        for (var i = 0; i < list.length; i++) {
            var item = list[i];
            var optSelected = item.Value == value ? 'selected' : '';
            var optDisabled = $.inArray(item.Value, excluded) > -1 ? 'disabled' : '';
            var option = '<option value="' + item.Value + '" ' + optSelected + ' ' + optDisabled + '>' + item.Text + '</option>';
            html += option;
        };
        html += '</select>';
        return html;
    }
}

/*
 * ComboBox类型的单选控件
 * @param url {string} 后台查询的地址：接受key为'query'的string参数，返回[{id: 'xxx', name: 'xxx'}, {...}]的json string
 * @param controlHolder {jQuery object} 显示控件的wrap对象
 * @param valueHolder {jQuery object} 存储已选值的input标签对象
 * @param Values {array} 显示的值，选择的值 需是{ id: '', name: ''}的对象数组
 * @param disabled {boolean} 是否禁用
*/
function CommonComboBoxSingleSelector(options) {
    var defaults = {
        url: '',
        controlHolder: null,
        valueHolder: null,
        Values: [],
        disabled: false,
        multiple: false,
        placeholder: '请输入关键字',
        params: {},
        onchange: null
    };
    var settings = $.extend({}, defaults, options);

    var selector = settings.controlHolder.magicSuggest({
        allowFreeEntries: false,
        allowDuplicates: false,
        maxSelection: settings.multiple ? null : 1,
        disabled: settings.disabled,
        method: 'post',
        queryParam: 'query',
        displayField: 'name',
        valueField: 'id',
        placeholder: settings.placeholder,
        data: settings.url,
        dataUrlParams: settings.params,
        selectionRenderer: function (data) { //想让标签里显示啥就看这个
            var n = data.name;
            var i = n.lastIndexOf(' | 输入: ');
            if (i >= 0 && i < n.length) {
                n = n.substr(0, i);
            }
            return n;
        }
    });

    // 初始化
    if (settings.Values && settings.Values.length > 0) {
        selector.setSelection(settings.Values);
    }

    $(selector).on('selectionchange', function (e, m) {
        var ids = [];
        var names = [];

        var selects = this.getSelection();
        if (selects && selects.length > 0) {
            for (var i = 0; i < selects.length; i++) {
                ids.push(selects[i].id);
                names.push(selects[i].name);
            }
        }

        var idsVal = ids.join(',');
        var namesVal = names.join('#$#');

        settings.valueHolder.data('id', idsVal);
        settings.valueHolder.data('names', namesVal);
        settings.valueHolder.val(idsVal).trigger('change');;

        if (typeof settings.onchange == 'function') {
            settings.onchange(ids);
        }
    });

    return {
        GetData: function () {
            return settings.valueHolder.val();
        },
        SetData: function (data) {
            selector.setSelection(data);
        }
    };
}

$(document).ready(function () {
    window.InitIBBComboSelector = function (obj, url, placeholder) {
        var controlHolder = obj.find('.control-holder');
        var valueHolder = obj.find('.value-holder');
        var disabled = valueHolder.data('disabled') == '1';
        var multiple = valueHolder.data('multiple') == '1';
        var extra = valueHolder.data('extra');

        var ids = $.trim(valueHolder.data('ids'));
        var names = $.trim(valueHolder.data('names'));
        var values = [];
        if (ids && names) {
            var idArr = ids.split(',');
            var nameArr = names.split('#$#');

            for (var i = 0; i < idArr.length; i++) {
                values.push({
                    id: idArr[i],
                    name: nameArr[i]
                });
            }
        }

        CommonComboBoxSingleSelector({
            url: url.toLowerCase(),
            controlHolder: controlHolder,
            valueHolder: valueHolder,
            Values: values,
            disabled: disabled,
            multiple: multiple,
            placeholder: placeholder,
            params: extra
        });
    }

    // ---- User
    window.InitUserSelector = function (obj) {
        InitIBBComboSelector(obj, '/CommonService/SearchUser/');
    }

    $('.user-selector').each(function () {
        var notinit = $(this).data('notinit') == '1';
        if (!notinit) {
            InitUserSelector($(this));
        }
    });

    // ---- Merchant
    window.InitMerchantSelector = function (obj) {
        InitIBBComboSelector(obj, '/CommonService/SearchMerchant/', '输入商家编号或编码或名称');
    }

    $('.merchant-selector').each(function () {
        var notinit = $(this).data('notinit') == '1';
        if (!notinit) {
            InitMerchantSelector($(this));
        }
    });

    // ---- SKU
    window.InitSKUSelector = function (obj) {
        InitIBBComboSelector(obj, '/CommonService/SearchSKU/', '输入物料编码或名称或条码');
    }

    $('.sku-selector').each(function () {
        var notinit = $(this).data('notinit') == '1';
        if (!notinit) {
            InitSKUSelector($(this));
        }
    });

    // ----- Item
    window.InitItemSelector = function (obj) {
        InitIBBComboSelector(obj, '/CommonService/SearchItem/', '输入商家的商品编码或名称或条码');
    }

    $('.item-selector').each(function () {
        var notinit = $(this).data('notinit') == '1';
        if (!notinit) {
            InitItemSelector($(this));
        }
    });

    // ----- Currency
    window.InitCurrencySelector = function (obj) {
        InitIBBComboSelector(obj, '/CommonService/SearchCurrency/');
    }
    $('.currency-selector').each(function () {
        var notinit = $(this).data('notinit') == '1';
        if (!notinit) {
            InitCurrencySelector($(this))
        }
    });

    // ----- Country
    window.InitCountrySelector = function (obj) {
        InitIBBComboSelector(obj, '/CommonService/SearchCountry/');
    }
    $('.country-selector').each(function () {
        var notinit = $(this).data('notinit') == '1';
        if (!notinit) {
            InitCountrySelector($(this))
        }
    });

    // ----- HSCode
    window.InitHSCodeSelector = function (obj) {
        InitIBBComboSelector(obj, '/CommonService/SearchHSCode/');
    }
    $('.hscode-selector').each(function () {
        var notinit = $(this).data('notinit') == '1';
        if (!notinit) {
            InitHSCodeSelector($(this))
        }
    });

    // ----- CIQCode
    window.InitCIQCodeSelector = function (obj) {
        InitIBBComboSelector(obj, '/CommonService/SearchCIQCode/');
    }
    $('.ciqcode-selector').each(function () {
        var notinit = $(this).data('notinit') == '1';
        if (!notinit) {
            InitCIQCodeSelector($(this))
        }
    });

    //window.InitBrandSelector = function(obj) {
    //	InitIBBComboSelector(obj, '/Brand/Search/');
    //}
    //$('.brand-selector').each(function() {
    //	var notinit = $(this).data('notinit') == '1';
    //	if (!notinit) {
    //		InitBrandSelector($(this));
    //	}
    //});

    //window.InitVendorSelector = function(obj) {
    //	InitIBBComboSelector(obj, '/Vendor/Search/');
    //}
    //$('.vendor-selector').each(function() {
    //	var notinit = $(this).data('notinit') == '1';
    //	if (!notinit) {
    //		InitVendorSelector($(this));
    //	}
    //});

    //$('.store-purchase-backlog-selector').each(function () {
    //    var notinit = $(this).data('notinit') == '1';
    //    if (!notinit) {
    //        InitIBBComboSelector($(this), '/Order/SearchStorePurchaseBacklog/');
    //    }
    //});

    //$('.order-task-selector').each(function () {
    //    var notinit = $(this).data('notinit') == '1';
    //    if (!notinit) {
    //        InitIBBComboSelector($(this), '/OrderTask/AjaxSearchTask/');
    //    }
    //});
    //window.InitOrderTaskSelector = function (obj) {
    //    InitIBBComboSelector(obj, '/OrderTask/AjaxSearchTask/');
    //}

    //window.InitCustomerSelector = function(obj) {
    //	InitIBBComboSelector(obj, '/CommonService/SearchCustomer/', '输入用户名/编号/手机号');
    //}
    //$('.customer-selector').each(function() {
    //	var notinit = $(this).data('notinit') == '1';
    //	if (!notinit) {
    //		InitCustomerSelector($(this));
    //	}
    //});

    //window.InitSalesAgentSelector = function(obj) {
    //	InitIBBComboSelector(obj, '/CommonService/SearchSalesAgent/', '输入代理商店铺名/编号/手机号');
    //}
    //$('.sales-agent-selector').each(function() {
    //	var notinit = $(this).data('notinit') == '1';
    //	if (!notinit) {
    //		InitSalesAgentSelector($(this));
    //	}
    //});

    //window.InitMemberNoticeContentTemplateSelector = function (obj) {
    //    InitIBBComboSelector(obj, '/MemberNotice/SearchContentTemplate/', '输入会员通知模板编号或名称');
    //}
    //$('.member-notice-content-template-selector').each(function () {
    //    var notinit = $(this).data('notinit') == '1';
    //    if (!notinit) {
    //        InitMemberNoticeContentTemplateSelector($(this));
    //    }
    //});

    //window.InitCouponSelector = function(obj) {
    //	InitIBBComboSelector(obj, '/CommonService/SearchCoupon/', '输入优惠券编号或名称');
    //}
    //$('.coupon-selector').each(function() {
    //	var notinit = $(this).data('notinit') == '1';
    //	if (!notinit) {
    //		InitCouponSelector($(this));
    //	}
    //});

    window.InitAreaSelector = function (obj) {
        var controlHolder = obj.find('.control-holder');
        var valueHolder = obj.find('.value-holder');
        var val = valueHolder.val();
        var disabled = valueHolder.data('disabled') == '1';
        var provinceSelectText = valueHolder.data('province-select-text');
        var citySelectText = valueHolder.data('city-select-text');
        var districtSelectText = valueHolder.data('district-select-text');

        return AreaSelector(controlHolder, valueHolder, val, disabled, provinceSelectText, citySelectText, districtSelectText);
    }
    $('.area-selector').each(function () {
        var notinit = $(this).data('notinit') == '1';
        if (!notinit) {
            InitAreaSelector($(this));
        }
    });
});

(function () {
    function ISearch(url, callback, parameters) {
        this.Url = url || '';
        this.Callback = callback || null;
        this.Parameters = parameters || {};
        this.Searching = false;
        this.NotSearched = '';
    }
    ISearch.prototype.Search = function (keyword) {
        var me = this;

        if (me.Searching) {
            me.NotSearched = keyword;
            return;
        }

        me.Searching = true;

        if (!me.Parameters) {
            me.Parameters = {};
        }
        me.Parameters.keyword = keyword;

        $.ajax({
            type: 'POST',
            dataType: 'json',
            url: me.Url,
            data: me.Parameters,
            success: function (data) {
                me.Searching = false;

                if (!data.error) {
                    var d1 = data.data;
                    d1.keyword = keyword;
                    me.Callback(d1);
                }

                var notSearched = me.NotSearched;
                if (notSearched) {
                    me.NotSearched = '';
                    me.Search(notSearched);
                }
            },
            error: function () {
                me.Searching = false;
            }
        });
    }
    window.ISearch = ISearch;

    function _ibbProductSelector() {
        // options
        this.multiple = false;
        this.callback = null;
        this.excluded = [];
        this.parameters = {};
        this.showBrandFilter = false;
        this.showCategoryFilter = false;

        this._getModalUrl = '/Views/Shared/Modals/ItemSelectorModal.cshtml'.toLowerCase();
        this._searchUrl = '/Item/AjaxSearch/'.toLowerCase();
        this._modalHtml = '';

        this._modal = null;

        this._mySearch = null;
        this._page = 1;
        this._totalPage = 1;

        this._resultList = [];
        this._selectedList = [];

        var me = this;
        this._replaceTpl = function (tpl, item) {
            return tpl.replace(/\{Id\}/g, item.Id)
                .replace(/\{Code\}/g, item.Code)
                .replace(/\{Name\}/g, item.Name)
                .replace(/\{SKUSourceTypeText\}/g, item.SKUSourceTypeText)
                .replace(/\{MerchantText\}/g, item.MerchantText);
        };
        this._searchSuccessCallback = function (data) {
            me._page = data.page;
            me._totalPage = data.totlaPage;
            var total_count = data.totalCount;

            var modal = me._modal;
            var list1 = modal.find('.result-list');
            var tpl = modal.find('.item-tpl-wrap1').html();

            list1.show().empty();
            me._resultList = [];
            if (data.list && data.list.length > 0) {
                for (var i = 0; i < data.list.length; i++) {
                    var item = data.list[i];
                    var html = me._replaceTpl(tpl, item);
                    var it1 = $(html);
                    list1.append(it1);
                    me._resultList.push(item);

                    if ($.inArray(item.Id, me.excluded) >= 0) {
                        it1.addClass('disabled');
                    }
                }
            }

            var items = list1.find('.item');
            items.off('click');
            items.on('click', function (event) {
                var tagName = event.target.tagName.toLowerCase();
                if (tagName != 'a') {
                    if ($(this).hasClass('disabled')) { return; }

                    var item = $(this).closest('.item');
                    var id = item.data('id');
                    if ($(this).hasClass('selected')) {
                        me._removeSelect(id);
                    } else {
                        me._addSelect(id);
                    }
                }
            });

            me._reRender();

            var pager = modal.find('.list-pager');
            var pager_btn = modal.find('.list-pager .pager-btn');

            var prev = pager.find('.btn-prev');
            var next = pager.find('.btn-next');
            if (total_count > 0) {
                modal.find('.list-pager .pager-text').html('当前为第' + me._page + '页（' + me._resultList.length + '条） / 共' + total_count + '条 / 共' + me._totalPage + '页');
                pager.show();
                if (me._totalPage > 1) {
                    pager_btn.show();
                }
                else {
                    pager_btn.hide();
                }
            }
            else {
                pager.hide();
            }

            me._page > 1 ? prev.removeClass('disabled') : prev.addClass('disabled');
            me._page < me._totalPage ? next.removeClass('disabled') : next.addClass('disabled');

            prev.off('click');
            prev.on('click', function () {
                if (prev.hasClass('disabled')) { return; }
                if (me._page > 1) { me._page--; }
                me._search();
            });
            next.off('click');
            next.on('click', function () {
                if (next.hasClass('disabled')) { return; }
                if (me._page < me._totalPage) { me._page++; }
                me._search();
            });
        };
        this._getItemInList = function (id, list) {
            var d1 = null;
            for (var i = 0; i < list.length; i++) {
                var item = list[i];
                if (item.Id == id) {
                    d1 = item;
                }
            }
            return d1;
        };
        this._addSelect = function (id) {
            var item1 = me._getItemInList(id, me._resultList);
            var item2 = me._getItemInList(id, me._selectedList);

            if (item1 && !item2) {
                me._selectedList.push(item1);
            }
            me._reRender();
            if (!me.multiple) {
                me._closeModal();
            }
        };
        this._removeSelect = function (id) {
            var item2 = me._getItemInList(id, me._selectedList);
            if (item2) {
                var index = -1;
                for (var i = 0; i < me._selectedList.length; i++) {
                    var item = me._selectedList[i];
                    if (item.Id == item2.Id) {
                        index = i;
                    }
                }
                if (index >= 0) {
                    me._selectedList.splice(index, 1);
                }
            }
            me._reRender();
        };
        this._reRender = function () {
            var modal = me._modal;
            modal.find('.result-list .item').each(function () {
                var id = $(this).data('id');
                var item = me._getItemInList(id, me._selectedList);
                if (item) {
                    $(this).addClass('selected');
                } else {
                    $(this).removeClass('selected');
                }
            });

            var list2 = modal.find('.selected-list');
            var tpl = modal.find('.item-tpl-wrap2').html();

            list2.empty();
            if (me._selectedList && me._selectedList.length > 0) {
                list2.show();
                for (var i = 0; i < me._selectedList.length; i++) {
                    var item = me._selectedList[i];
                    var html = me._replaceTpl(tpl, item);
                    list2.append(html);
                }
                modal.find('.selected-count-txt').html('（' + me._selectedList.length + '条）');
            } else {
                list2.hide();
                modal.find('.selected-count-txt').html('');
            }

            var closes = list2.find('.item .btn-close');
            closes.off('click');
            closes.on('click', function (event) {
                var item = $(this).closest('.item');
                var id = item.data('id');
                me._removeSelect(id);
            });
        };
        this._reset = function () {
            me._modal = null;

            me._mySearch = null;
            me._page = 1;
            me._totalPage = 1;

            this._resultList = [];
            this._selectedList = [];
        };
        this._showModal = function () {
            me._reset();
            var modal = $(me._modalHtml); //$('#itemSelectorModalId');
            $('body').append(modal);
            modal.modal('show');
            modal.on('shown.bs.modal', function () {
                modal.find('.ipt-name').focus();
                modal.find('.merchant-selector').each(function () {
                    InitMerchantSelector($(this));
                });
            });
            modal.on('hidden.bs.modal', function () {
                modal.remove();
            });
            me._modal = modal;

            me._mySearch = new ISearch(me._searchUrl, me._searchSuccessCallback);

            modal.find('.ipt-name').bind('input propertychange', function () {
                me._research();
            });

            modal.find('#slt-merchant').change(function () { me._research(); });
            modal.find('.sku-source-type-list input.chk-stype').change(function () { me._research(); });

            if (!me.multiple) {
                modal.find('.modal-footer').hide();
                modal.find('.selected-list-wrap').hide();
            }
            modal.find('.btn-confirm').click(function () {
                me._closeModal();
            });
        };
        this._research = function () {
            me._page = 1;
            me._search();
        };
        this._search = function () {
            var modal = me._modal;

            var keyword = $.trim(modal.find('.ipt-name').val());
            if (!keyword) { //没有关键字，不搜索
                return;
            }

            me.parameters.merchantId = modal.find('#slt-merchant').val();

            var stypes = [];
            var stfilter = modal.find('.sku-source-type-list');
            stfilter.find('input.chk-stype:checked').each(function () {
                stypes.push($(this).val());
            });
            me.parameters.skuSourceTypes = stypes.join(',');

            me.parameters.page = me._page;

            me._mySearch.Parameters = me.parameters;

            me._mySearch.Search(keyword);
        };
        this._closeModal = function () {
            var modal = me._modal;
            var errorMsg = modal.find('.error-msg');
            errorMsg.html('');

            if (me._selectedList && me._selectedList.length > 0) {
                modal.modal('hide');

                if (typeof me.callback == 'function') {
                    if (me.multiple) {
                        me.callback(me._selectedList);
                    } else {
                        me.callback(me._selectedList[0]);
                    }
                }
            } else {
                errorMsg.html('请选择商品！')
            }
        }
    }
    _ibbProductSelector.prototype.Select = function (options) {
        var me = this;
        if (options) {
            me.multiple = options.multiple || false;
            me.callback = options.callback || null;
            me.excluded = options.excluded || [];
            me.parameters = options.parameters || {};

            me.initSKUSourceTypes = options.initSKUSourceTypes || [];
            me.disableSKUSourceTypes = options.disableSKUSourceTypes || false;

            me.initMerchantId = options.initMerchantId || null;
            me.disableMerchant = options.disableMerchant || false;
        }

        var loadUrl = '/commonservice/loadpartialviewform/?viewpath=' + encodeURIComponent(this._getModalUrl);
        IBBAlert.ShowLoadingLayer();
        $.ajax({
            type: 'POST',
            dataType: 'json',
            url: loadUrl,
            data: {
                initSKUSourceTypes: me.initSKUSourceTypes.join(','),
                disableSKUSourceTypes: me.disableSKUSourceTypes,
                initMerchantId: me.initMerchantId,
                disableMerchant: me.disableMerchant
            },
            success: function (data) {
                IBBAlert.ShowLoadingLayer(true);
                if (!data.error) {
                    me._modalHtml = data.data.html;
                    me._showModal();
                }
            },
            error: function () {
                IBBAlert.ShowLoadingLayer(true);
            }
        });
    }
    window.IBBProductSelector = new _ibbProductSelector();
})();
