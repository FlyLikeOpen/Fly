(function($) {
	function _ibbAlert() {
		var _alert = function (msg, callback) {
			var type = msg.type.toUpperCase();
			if (type != 'ERROR' && type != 'SUCCESS' && type != 'INFO') {
				type = 'INFO';
			}
			var fa = '';
			var bg = '';
			if (type == 'ERROR') {
				fa = 'fa-warning';
				bg = 'bg-danger text-danger';
			} else if (type == 'SUCCESS') {
				fa = 'fa-check';
				bg = 'bg-success text-success';
			} else if (type == 'INFO') {
				fa = 'fa-info-circle';
				bg = 'bg-info text-info';
			}
			
			var mid = GenerateUniqueId('AlertModal');
			var html = [
				'<div class="modal fade" id="'+mid+'" tabindex="-1" role="dialog">',
					'<div class="modal-dialog">',
						'<div class="modal-content">',
							'<div class="modal-header '+bg+'" style="border-radius: 6px 6px 0 0;">',
								'<button type="button" class="close" data-dismiss="modal" aria-label="Close"><span aria-hidden="true">&times;</span></button>',
								'<h4 class="modal-title"><i class="fa '+fa+'"></i> '+msg.title+'</h4>',
							'</div>',
							'<div class="modal-body">'+msg.text+'</div>',
							'<div class="modal-footer">',
								'<span class="result-message"></span>',
								'<button type="button" class="btn btn-default" data-dismiss="modal">'+msg.cancelBtn+'</button>',
							'</div>',
						'</div>',
					'</div>',
				'</div>'
			].join('');

			$('body').append(html);
			var modal = $('#' + mid);
			modal.modal('show');

			modal.on('hidden.bs.modal', function() {
				modal.remove();
				if (typeof callback == 'function') {
					callback();
				}
			});

			modal.on('shown.bs.modal', function () {
				var mbody = modal.find('.modal-body');
				if (mbody.height() > 500) {
					mbody.css({
						'max-height': '500px',
						'overflow-y': 'auto',
						'word-wrap': 'break-word'
					});
				}
				modal.find('.modal-footer button').focus();
			});
		}
		this.Error = function (text, callback) {
			var msg = {
				'type': 'ERROR',
				'title': '错误',
				'text': text,
				'cancelBtn': '确定'
			};
			_alert(msg, callback);
		}
		this.Success = function (text, callback) {
			var msg = {
				'type': 'SUCCESS',
				'title': '成功',
				'text': text,
				'cancelBtn': '确定'
			};
			_alert(msg, callback);
		}
		this.Info = function (text, callback) {
			var msg = {
				'type': 'INFO',
				'title': '提示',
				'text': text,
				'cancelBtn': '确定'
			};
			_alert(msg, callback);
		}
		this.Confirm = function (msg, callback) {
			var mid = 'ConfirmModal_' + (Math.random() + '').replace('.', '')
			var html = [
				'<div class="modal fade" id="'+mid+'" tabindex="-1" role="dialog">',
					'<div class="modal-dialog">',
						'<div class="modal-content">',
							'<div class="modal-header">',
								'<button type="button" class="close" data-dismiss="modal" aria-hidden="true">&times;</button>',
								'<h4 class="modal-title"><i class="fa fa-info-circle"></i> 确认提示</h4>',
							'</div>',
							'<div class="modal-body"><p>' + msg + '</p></div>',
							'<div class="modal-footer">',
								'<span class="result-message"></span>',
								'<button type="button" class="btn btn-default cancel-btn">取消</button>',
								'<button type="button" class="btn btn-primary ok-btn">确定</button>',
							'</div>',
						'</div>',
					'</div>',
				'</div>',
			].join('');

			$('body').append(html);
			var modal = $('#' + mid);
			modal.modal('show');

			modal.find('.cancel-btn').click(function () {
				modal.modal('hide');
				if (typeof callback == 'function') {
					callback(false);
				}
			});
			modal.find('.ok-btn').click(function () {
				modal.modal('hide');
				if (typeof callback == 'function') {
					callback(true);
				}
			});
			modal.on('hidden.bs.modal', function () {
				modal.remove();
			});
		}
		this.InputBox = function (msg, required, callback, singleline, defaultText) {
		    var mid = 'InputBoxModal_' + (Math.random() + '').replace('.', '')
		    var textid = mid + '_text';
		    var tipid = mid + '_tip';
		    var singlelineFlag = typeof (singleline) == 'boolean' && singleline == true;
		    var dtxt = Object.prototype.toString.call(defaultText) === '[object String]' ? defaultText : '';
		    var controlHtml = '';
		    if (singlelineFlag) { // 单行文本输入
		        controlHtml = '<input onfocus="$(\'#' + tipid + '\').html(\'\');" class="form-control long" id="' + textid + '" value="' + dtxt + '"></textarea>';
		    }
		    else { // 多行文本输入
		        controlHtml = '<textarea onfocus="$(\'#' + tipid + '\').html(\'\');" class="form-control long" rows="5" id="' + textid + '">' + dtxt + '</textarea>';
		    }
		    var html = [
				'<div class="modal fade" id="' + mid + '" tabindex="-1" role="dialog">',
					'<div class="modal-dialog">',
						'<div class="modal-content">',
							'<div class="modal-header">',
								'<button type="button" class="close" data-dismiss="modal" aria-hidden="true">&times;</button>',
								'<h4 class="modal-title"><i class="fa fa-info-circle"></i> 确认提示</h4>',
							'</div>',
							'<div class="modal-body"><p>' + msg + '</p><p>' + controlHtml + '</p><p id="' + tipid + '" style="color: red"></p></div>',
							'<div class="modal-footer">',
								'<span class="result-message"></span>',
								'<button type="button" class="btn btn-default cancel-btn">取消</button>',
								'<button type="button" class="btn btn-primary ok-btn">确定</button>',
							'</div>',
						'</div>',
					'</div>',
				'</div>',
		    ].join('');

		    $('body').append(html);
		    var modal = $('#' + mid);
		    modal.modal('show');

		    modal.find('.cancel-btn').click(function () {
		        var input = $('#' + textid).val();
		        modal.modal('hide');
		        if (typeof callback == 'function') {
		            callback(false, input);
		        }
		    });
		    modal.find('.ok-btn').click(function () {
		        var input = $('#' + textid).val();
		        if (required && !input) {
		            $('#' + tipid).html('<i class="fa fa-exclamation-triangle"></i> 内容不能为空');
		            return;
		        }
		        modal.modal('hide');
		        if (typeof callback == 'function') {
		            callback(true, input);
		        }
		    });
		    modal.on('hidden.bs.modal', function () {
		        modal.remove();
		    });
		    return modal;
		}
		this.InfoBox = function (text, callback) {
		    var msg = {
		        'type': 'INFO',
		        'title': '提示',
		        'text': text,
		        'cancelBtn': '确定'
		    };
		    var type = msg.type.toUpperCase();
		    if (type != 'ERROR' && type != 'SUCCESS' && type != 'INFO') {
		        type = 'INFO';
		    }
		    var fa = '';
		    var bg = '';
		    if (type == 'ERROR') {
		        fa = 'fa-warning';
		        bg = 'bg-danger text-danger';
		    } else if (type == 'SUCCESS') {
		        fa = 'fa-check';
		        bg = 'bg-success text-success';
		    } else if (type == 'INFO') {
		        fa = 'fa-info-circle';
		        bg = 'bg-info text-info';
		    }

		    var mid = 'AlertBoxModal_' + (Math.random() + '').replace('.', '');
		    var html = [
				'<div class="modal fade" id="' + mid + '" tabindex="-1" role="dialog">',
					'<div class="modal-dialog">',
						'<div class="modal-content">',
							'<div class="modal-header ' + bg + '" style="border-radius: 6px 6px 0 0;">',
								'<button type="button" class="close" data-dismiss="modal" aria-label="Close"><span aria-hidden="true">&times;</span></button>',
								'<h4 class="modal-title"><i class="fa ' + fa + '"></i> ' + msg.title + '</h4>',
							'</div>',
							'<div class="modal-body"><p><textarea class="form-control long" rows="5" readonly="readonly">' + msg.text + '</textarea></p></div>',
							'<div class="modal-footer">',
								'<span class="result-message"></span>',
								'<button type="button" class="btn btn-default" data-dismiss="modal">' + msg.cancelBtn + '</button>',
							'</div>',
						'</div>',
					'</div>',
				'</div>'
		    ].join('');

		    $('body').append(html);
		    var modal = $('#' + mid);
		    modal.modal('show');

		    modal.on('hidden.bs.modal', function () {
		        modal.remove();
		        if (typeof callback == 'function') {
		            callback();
		        }
		    });

		    modal.on('shown.bs.modal', function () {
		        modal.find('.modal-footer button').focus();
		    });
		}
		// 弹出一个loading层，用于进行长时间操作（如Ajax）时的显示
		this.ShowLoadingLayer = function(isClose) {
			var id = 'TmpIDForLoadingLayer123';
			var dd = $('#'+id);

			if (isClose == true) {
				if (dd.length > 0) {
					dd.stop().fadeOut(50, function() {
						dd.remove();
					});
				}
			} else {
				if (dd.length <= 0) {
					var html = [
						'<div id="'+id+'" style="display:none;">',
							'<div class="inner"><img src="/resources/images/loading2.gif" /></div>',
						'</div>'
					].join('');
					$('body').append(html);
					var dd = $('#'+id);
					dd.stop().fadeIn(200);
				}
			}
		};
		// Ajax加载远程的Bootstrap Modal Html，显示出来后调用callback
		this.LoadRemoteModal = function (loadUrl, postData, callback) {
		    loadUrl = loadUrl.toLowerCase();
		    if (loadUrl.indexOf('/views/') == 0) {
		        loadUrl = '/commonservice/loadpartialviewform/?viewpath=' + encodeURIComponent(loadUrl);
		    }
			IBBAlert.ShowLoadingLayer();
			$.ajax({
				type: 'POST',
				dataType: 'json',
				url: loadUrl.toLowerCase(),
				data: postData,
				success: function(data) {
					IBBAlert.ShowLoadingLayer(true);
					if (!data.error) {
						var modal = $(data.data.html);
						$('body').append(modal);

						modal.on('hidden.bs.modal', function() {
							modal.remove();
						});
						
						modal.on('shown.bs.modal', function() {
							if (typeof callback == 'function') {
								callback(modal);
							}
						});
						
						modal.modal('show');
					}
				},
				error: function() {
					IBBAlert.ShowLoadingLayer(true);
				}
			});
		}
	}
	window.IBBAlert = new _ibbAlert();
})(jQuery);

(function($) {
	$.fn.extend({
		numberInput: function(min, max, allowFloat) {
			min = typeof min == 'number' ? min : 1 - Number.MAX_VALUE;
			max = typeof max == 'number' ? max : Number.MAX_VALUE;
			allowFloat = typeof allowFloat == 'boolean' ? allowFloat : false;

			var ipt = $(this);
			ipt.blur(function() {
			    var num = $.trim(ipt.val());
			    var orignalV = num;
				num = num.replace(/[^\d\.\-]/g, '');

				if (num == '') {
				    ipt.val(num);
				    if (orignalV != num) {
				        ipt.blur();
				    }
					return;
				}
				if (isNaN(num)) { num = 0; }
				num = allowFloat ? parseFloat(num) : parseInt(num);
				if (num < min) { num = min; }
				if (num > max) { num = max; }

				ipt.val(num);
				if (orignalV != num) {
				    ipt.blur();
				}
			});
		},
		positiveNumber: function () {
			var max = null;
			var txt = $.trim($(this).data('max'));
			if (txt != '' && !isNaN(txt)) {
				max = parseFloat(txt);
			}
			$(this).numberInput(0, max, true);
		},
		positiveInt: function() {
			var max = null;
			var txt = $.trim($(this).data('max'));
			if (txt != '' && !isNaN(txt)) {
				max = parseFloat(txt);
			}
			$(this).numberInput(1, max, false);
		},
		positiveZeroInt: function() {
			var max = null;
			var txt = $.trim($(this).data('max'));
			if (txt != '' && !isNaN(txt)) {
				max = parseFloat(txt);
			}
			$(this).numberInput(0, max, false);
		},
		positiveNegativeInt: function() {
			var min = null;
			var txt1 = $.trim($(this).data('min'));
			if (txt1 != '' && !isNaN(txt1)) {
				min = parseFloat(txt1);
			}
			var max = null;
			var txt2 = $.trim($(this).data('max'));
			if (txt2 != '' && !isNaN(txt2)) {
				max = parseFloat(txt2);
			}
			$(this).numberInput(min, max, false);
		},
		priceInput: function(max) {
			min = 0;
			max = typeof max == 'number' ? max : Number.MAX_VALUE;

			var ipt = $(this);
			ipt.blur(function() {
				var num = $.trim(ipt.val());
				var orignalV = num;
				num = num.replace(/[^\d\.\-]/g, '');

				if (num == '') {
					ipt.val(num);
					if (orignalV != num) {
						ipt.blur();
					}
					return;
				}
				if (isNaN(num)) { num = 0; }
				
				num = parseFloat(num);

				if (num < min) { num = min; }
				if (num > max) { num = max; }

				num = num.toFixed(2);
				ipt.val(num);
				if (orignalV != num) {
					ipt.blur();
				}
			});
		},
	});
	
	$(document).ready(function() {
		// 只能输入正数
		$('.positive-number').each(function() {
			$(this).positiveNumber();
		});
		//只能输入正整数
		$('.positive-int').each(function() {
			$(this).positiveInt();
		});
		//只能输入整数
		$('.positive-negative-int').each(function() {
			$(this).positiveNegativeInt();
		});
		//只能输入0,1,2,3...的>=0的整数
		$('.positive-zeroint').each(function() {
			$(this).positiveZeroInt();
		});
		//只能输入数字
		$('.float-number').each(function () {
			$(this).numberInput(1 - Number.MAX_VALUE, Number.MAX_VALUE,true);
		});
		// 价格
		$('.price-input').each(function() {
			var max = $(this).data('max');
			$(this).priceInput(max);
		});
	});
})(jQuery);


function ShowInventoryAllocationSelectorForm(callback) {
    IBBAlert.LoadRemoteModal('/Views/InventoryAllocation/Partial/ChooseItemForm.cshtml', null, function (modal) {
        modal.find('#pkeywordIpt').keyup(function () {
            var w = $.trim($(this).val());
            modal.find('#allocationGrid tbody tr').show();
            if (w.length > 0) {
                var keywordList = w.split(' ');
                var trs = modal.find('#allocationGrid .allocationItem');
                for (var i = 0; i < trs.length; i++) {
                    var c = $(trs[i]).find('.product').html();
                    var hit = true;
                    for (var j = 0; j < keywordList.length; j++) {
                        if (c.indexOf(keywordList[j]) < 0) {
                            hit = false;
                            break;
                        }
                    }
                    if (hit == false) {
                        $(trs[i]).hide();
                        modal.find('#desc_' + $(trs[i]).data('id')).hide();
                    }
                }
            }
        });
        var grid = window.IBBGridCheck2('#allocationGrid');        
        modal.find('.btn-select').click(function () {            
            var list = grid.getChecked();
            var objs = [];            
            for (var i = 0; i < list.length; i++) {
                var tr = modal.find('#tr_' + list[i]);
                if (tr.is(':visible')) {
                    objs.push({
                        idNumber: tr.find('.idNumber').html(),
                        product: tr.find('.product').html(),
                        stock: tr.find('.stock').html(),
                        qty: parseInt(tr.find('.qty').html())
                    });
                }
            }
            if (objs.length <= 0) {
                IBBAlert.Error('请先选择要使用的占用库存');
                return;
            }
            modal.modal('hide');
            if (callback && typeof (callback) == 'function') {
                callback(objs);
            }
        });
    });
}

function ShowProductSupplySelectorForm(productId, salesChannelNo, defaultSupplyId, qty, allowSelfOperation, callback) {
    if (!salesChannelNo || salesChannelNo == '0' || salesChannelNo == 0) {
        IBBAlert.Error('请先指定销售渠道后才能选择供货关系');
        return;
    }
    IBBAlert.LoadRemoteModal('/Views/SupplyInfo/Partial/SupplySelectorForm.cshtml', { productId: productId, salesChannelNo: salesChannelNo, defaultSupplyId: defaultSupplyId, qty: qty, allowSelfOperation: allowSelfOperation }, function (modal) {
        $('[data-toggle="tooltip"]').tooltip();
        modal.find('.self-op').click(function () { // 选择了自营
            modal.modal('hide');
            if (callback && typeof (callback) == 'function') {
                callback(null);
            }
        });
        modal.find('.selected-btn').click(function () {
            var json = $(this).next('textarea').val();
            //alert(json);
            
            if (callback && typeof (callback) == 'function') {
                var rst = callback(eval("(" + json + ")"));
                if (typeof (rst) != 'boolean' || rst == null || rst != false) {
                    modal.modal('hide');
                }
            }
            else {
                modal.modal('hide');
            }
        });
    });
}

// 生成一个随机的ID字符串
function GenerateUniqueId(base) {
	base = base || 'RandomId';
	return base + '_' + (new Date()).getTime() + '_' + parseInt(Math.random() * 10000);
}

// 格式化金额显示
// FormatMoney('12345.676910', 2)将返回字符串12,345.68
// FormatMoney('12345.6', 2)将返回字符串12,345.60
function FormatMoney(s, n) {
    n = n > 0 && n <= 20 ? n : 2;
    s = parseFloat((s + "").replace(/[^\d\.-]/g, "")).toFixed(n) + "";
    var l = s.split(".")[0].split("").reverse(),
    r = s.split(".")[1];
    t = "";
    for (i = 0; i < l.length; i++) {
        t += l[i] + ((i + 1) % 3 == 0 && (i + 1) != l.length ? "," : "");
    }
    return t.split("").reverse().join("") + "." + r;
}

// 将金额显示方式的字符串转化为浮点数
// ParseMoneyToFloat('12,345.60') 将返回浮点数12345.6
function ParseMoneyToFloat(s) {
    return parseFloat(s.replace(/[^\d\.-]/g, ""));
}

var InitPhotoSwipeFromDOM = function (gallerySelector) {
	// parse slide data (url, title, size ...) from DOM elements 
	// (children of gallerySelector)
	var parseThumbnailElements = function (el) {
		var thumbElements = el.childNodes,
	        numNodes = thumbElements.length,
	        items = [],
	        figureEl,
	        childElements,
	        linkEl,
	        size,
	        item;

		for (var i = 0; i < numNodes; i++) {


			figureEl = thumbElements[i]; // <figure> element

			// include only element nodes 
			if (figureEl.nodeType !== 1) {
				continue;
			}

			linkEl = figureEl.children[0]; // <a> element

			size = linkEl.getAttribute('data-size').split('x');

			// create slide object
			item = {
				src: linkEl.getAttribute('href'),
				w: parseInt(size[0], 10),
				h: parseInt(size[1], 10)
			};



			if (figureEl.children.length > 1) {
				// <figcaption> content
				item.title = figureEl.children[1].innerHTML;
			}

			if (linkEl.children.length > 0) {
				// <img> thumbnail element, retrieving thumbnail url
				item.msrc = linkEl.children[0].getAttribute('src');
			}

			item.el = figureEl; // save link to element for getThumbBoundsFn
			items.push(item);
		}

		return items;
	};

	// find nearest parent element
	var closest = function closest(el, fn) {
		return el && (fn(el) ? el : closest(el.parentNode, fn));
	};

	// triggers when user clicks on thumbnail
	var onThumbnailsClick = function (e) {
		e = e || window.event;
		e.preventDefault ? e.preventDefault() : e.returnValue = false;

		var eTarget = e.target || e.srcElement;

		var clickedListItem = closest(eTarget, function (el) {
			return (el.tagName && el.tagName.toUpperCase() === 'FIGURE');
		});

		if (!clickedListItem) {
			return;
		}


		// find index of clicked item
		var clickedGallery = clickedListItem.parentNode,
	    	childNodes = clickedListItem.parentNode.childNodes,
	        numChildNodes = childNodes.length,
	        nodeIndex = 0,
	        index;
		for (var i = 0; i < numChildNodes; i++) {
			if (childNodes[i].nodeType !== 1) {
				continue;
			}

			if (childNodes[i] === clickedListItem) {
				index = nodeIndex;
				break;
			}
			nodeIndex++;
		}

		if (index >= 0) {
			openPhotoSwipe(index, clickedGallery);
		}
		return false;
	};

	// parse picture index and gallery index from URL (#&pid=1&gid=2)
	var photoswipeParseHash = function () {
		var hash = window.location.hash.substring(1),
	    params = {};

		if (hash.length < 5) {
			return params;
		}

		var vars = hash.split('&');
		for (var i = 0; i < vars.length; i++) {
			if (!vars[i]) {
				continue;
			}
			var pair = vars[i].split('=');
			if (pair.length < 2) {
				continue;
			}
			params[pair[0]] = pair[1];
		}

		if (params.gid) {
			params.gid = parseInt(params.gid, 10);
		}

		if (!params.hasOwnProperty('pid')) {
			return params;
		}
		params.pid = parseInt(params.pid, 10);
		return params;
	};

	var openPhotoSwipe = function (index, galleryElement, disableAnimation) {
		var pswpElement = document.querySelectorAll('.pswp')[0],
	        gallery,
	        options,
	        items;

		items = parseThumbnailElements(galleryElement);

		// define options (if needed)
		options = {
			index: index,

			// define gallery index (for URL)
			galleryUID: galleryElement.getAttribute('data-pswp-uid'),

			getThumbBoundsFn: function (index) {
				// See Options -> getThumbBoundsFn section of docs for more info
				var thumbnail = items[index].el.getElementsByTagName('img')[0], // find thumbnail
	                pageYScroll = window.pageYOffset || document.documentElement.scrollTop,
	                rect = thumbnail.getBoundingClientRect();

				return { x: rect.left, y: rect.top + pageYScroll, w: rect.width };
			},

			// history & focus options are disabled on CodePen
			// remove these lines in real life: 
			historyEnabled: false,
			focus: false

		};

		if (disableAnimation) {
			options.showAnimationDuration = 0;
		}
		// Pass data to PhotoSwipe and initialize it
		gallery = new PhotoSwipe(pswpElement, PhotoSwipeUI_Default, items, options);
		gallery.init();
	};

	// loop through all gallery elements and bind events
	var galleryElements = document.querySelectorAll(gallerySelector);

	for (var i = 0, l = galleryElements.length; i < l; i++) {
		galleryElements[i].setAttribute('data-pswp-uid', i + 1);
		galleryElements[i].onclick = onThumbnailsClick;
	}

	// Parse URL and open gallery if it contains #&pid=3&gid=1
	var hashData = photoswipeParseHash();
	if (hashData.pid > 0 && hashData.gid > 0) {
		openPhotoSwipe(hashData.pid - 1, galleryElements[hashData.gid - 1], true);
	}
};

$(document).ready(function(e){
	if ($('.my-simple-gallery').length > 0) {
		InitPhotoSwipeFromDOM('.my-simple-gallery');
	}
})

function NewOrEditShippingAddress(options) {
	var customerId = options.customerId; // 新建地址时传入的customerId
	var addressId = options.addressId; // 编辑地址时传入的Id
	var callback = options.callback; // 回调方法

	var _showForm = function(html) {
		$('body').append(html);
		var modal = $('#NewOrEditAddressModalID');
		modal.on('hidden.bs.modal', function() { modal.remove(); });
		modal.modal('show');

		modal.find('.area-selector').each(function() { InitAreaSelector($(this)); });

		var _showError = function(msg) {
			if (msg) {
				IBBAlert.Error(msg, true);
			}
		}

		modal.find('.btn-submit').click(function() {
			var contractName = $.trim(modal.find('.ipt-contractName').val());
			var areaId = $.trim(modal.find('.ipt-areaId').val());
			var streetAddress = $.trim(modal.find('.ipt-streetAddress').val());
			var mobile = $.trim(modal.find('.ipt-mobile').val());
			var identityCardNo = $.trim(modal.find('.ipt-identityCardNo').val());

			if (!contractName) { _showError('请填写收件人姓名！'); return false; }
			if (!areaId) { _showError('请选择收件地址并精确到区！'); return false; }
			if (!streetAddress) { _showError('请填写详细地址！'); return false; }
			if (!mobile) { _showError('请填写联系电话！'); return false; }

			IBBAlert.ShowLoadingLayer();
			$.ajax({
				type: 'POST',
				dataType: 'json',
				url: '/Customer/NewOrEditAddress/'.toLowerCase(),
				data: {
					customerId: customerId,
					addressId: addressId,
					contractName: contractName,
					areaId: areaId,
					streetAddress: streetAddress,
					mobile: mobile,
					identityCardNo: identityCardNo,
					setAsDefault: modal.find('.chk-default').prop('checked')
				},
				success: function(data) {
					IBBAlert.ShowLoadingLayer(true);
					if (!data.error) {
						modal.modal('hide');
						if (typeof callback == 'function') {
							var address = data.data.Address;
							address.FullAddress = data.data.FullAddress;
							callback(address);
						}
					}
				},
				error: function() {
					IBBAlert.ShowLoadingLayer(true);
				}
			});
		});
	}

	IBBAlert.ShowLoadingLayer();
	$.ajax({
		type: 'POST',
		dataType: 'json',
		url: '/Customer/LoadNewOrEditAddressModalHtml/'.toLowerCase(),
		data: { addressId: addressId },
		success: function(data) {
			IBBAlert.ShowLoadingLayer(true);
			if (!data.error) {
				_showForm(data.data.html);
			}
		},
		error: function() {
			IBBAlert.ShowLoadingLayer(true);
		}
	});
}

function RemoveAddress(addressId, callback) {
	IBBAlert.Confirm('确定删除该地址？', function(yes) {
		if (yes) {
			$.ajax({
				type: 'POST',
				dataType: 'json',
				url: '/Customer/RemoveShippingAddress/'.toLowerCase(),
				data: { addressId: addressId },
				success: function(data) {
					if (!data.error) {
						if (typeof callback == 'function') {
							callback();
						}
					}
				}
			});
		}
	});
}

function ShippingAddressSelector(options) {
	var customerId = options.customerId; // 客户ID
	var callback = options.callback; // 回调方法

	var _showForm = function(html) {
		$('body').append(html);
		var modal = $('#AddressSelectorModalID');
		modal.on('hidden.bs.modal', function() { modal.remove(); });
		modal.modal('show');
		
		var addressList = modal.find('.address-list');
		var noAddress = modal.find('.no-address');

		var newOrEditAddressCallback = function(address) {
			if (address) {
				$.ajax({
					type: 'POST',
					dataType: 'json',
					url: '/Customer/GetAddressLineHtml/'.toLowerCase(),
					data: { addressId: address.Id },
					success: function(data) {
						if (!data.error) {
							if (address.IsDefault) {
								addressList.find('.address-line .is-default').remove();
							}

							var item = $(data.data.html);
							var card1 = addressList.find('.address-line#' + address.Id);
							if (card1 && card1.length > 0) {
								card1.replaceWith(item);
							} else {
								addressList.append(item);
							}
							fun1();
						}
					}
				});
			} else {
				fun1();
			}
		};
		var fun1 = function() {
			addressList.show();
			if (addressList.find('.address-line').length > 0) {
				noAddress.hide();
			} else {
				noAddress.show();
			}
		}

		modal.find('.btn-newaddress').click(function() {
			NewOrEditShippingAddress({
				customerId: customerId,
				addressId: null,
				callback: newOrEditAddressCallback
			});
		});
		addressList.on('click', '.address-line', function(e) {
			var line = $(this).closest('.address-line');
			var target = $(e.target);
			var addressId = line.data('id');

			if (target.hasClass('btn-edit')) {
				NewOrEditShippingAddress({
					customerId: customerId,
					addressId: addressId,
					callback: newOrEditAddressCallback
				});
			} else if (target.hasClass('btn-remove')) {
				RemoveAddress(addressId, function() {
					line.remove();
					fun1();
				});
			} else {
				modal.modal('hide');

				var data1 = line.find('.data-holder').text();
				var address = JSON.parse(data1);
				if (typeof callback == 'function') {
					callback(address);
				}
			}
		});
	}

	IBBAlert.ShowLoadingLayer();
	$.ajax({
		type: 'POST',
		dataType: 'json',
		url: '/Customer/LoadAddressSelectorModalHtml/'.toLowerCase(),
		data: { customerId: customerId },
		success: function(data) {
			IBBAlert.ShowLoadingLayer(true);
			if (!data.error) {
				_showForm(data.data.html);
			}
		},
		error: function() {
			IBBAlert.ShowLoadingLayer(true);
		}
	});
}

/*
var options = {
	initPointLat: 30.657929,
	initPointLng: 104.064890,
	callback: function() {}
};
*/
function MapPickPosition(options) {
	function _QQMap(moptions) {
		var domId = moptions.domId ? moptions.domId : 'container';
		var initPointLat = moptions.initPointLat ? moptions.initPointLat : null;
		var initPointLng = moptions.initPointLng ? moptions.initPointLng : null;

		var mapCenterLat = initPointLat ? initPointLat : 30.657929;
		var mapCenterLng = initPointLng ? initPointLng : 104.064890;

		var _map = null; // 地图实例对象
		var _markers = [];
		var _searchService = null;
		var _selectedMarker = null;

		var _buildMarker = function(mapObj, latLngObj) {
			return new qq.maps.Marker({
				map: mapObj,
				clickable: true,
				draggable: true,
				position: latLngObj
			});
		}
		var _selectMarker = function(mk) {
			for (var i = 0; i < _markers.length; i++) {
				_markers[i].setIcon('');
			}

			_selectedMarker = mk;
			mk.setIcon('/resources/images/map-marker-red.png');
		}
		var _doSearch = function() {
			var overlay;
			while(overlay = _markers.pop()){
				overlay.setMap(null);
			}
			_markers = [];
			_selectedMarker = null;
			
			var keyword = document.getElementById('map-keyword').value;
			var region = document.getElementById('map-region').value;

			_searchService.setLocation(region);
			_searchService.search(keyword);
		}
		var _getPosition = function() {
			if (_selectedMarker) {
				var p = _selectedMarker.getPosition();
				var lat = p.getLat(); // 纬度值
				var lng = p.getLng(); // 经度值
				return {
					Latitude: lat.toFixed(5),
					Longitude: lng.toFixed(5)
				};
			}
			return null;
		}

		// 初始化地图
		_map = new qq.maps.Map(document.getElementById(domId), {
			center: new qq.maps.LatLng(mapCenterLat, mapCenterLng),
			zoom: 11
		});

		var latlngBounds = new qq.maps.LatLngBounds();
		_searchService = new qq.maps.SearchService({
			complete : function(results) {
				if (results && results.detail && results.detail.pois) {
					var pois = results.detail.pois;
					for (var i = 0,l = pois.length; i < l; i++) {
						var poi = pois[i];
						latlngBounds.extend(poi.latLng);  
						
						var marker = _buildMarker(_map, poi.latLng);
						_markers.push(marker);
						
						qq.maps.event.addListener(marker, 'click', function(event) {
							_selectMarker(event.target);
						});
					}
					_map.fitBounds(latlngBounds);
				}
			}
		});

		// 为地图添加一些控件
		var style = 'padding: 5px; border: 2px #86ACF2 solid; background: #FFF; cursor: pointer;';

		var ctrl1 = document.createElement('div');
		ctrl1.style.cssText = style;
		ctrl1.innerHTML = '<input type="text" name="map-keyword" id="map-keyword" placeholder="输入地址搜索" style="width: 200px;" />';
		ctrl1.index = 1;
		ctrl1.style.margin = '0 5px 0 0';
		_map.controls[qq.maps.ControlPosition.TOP_CENTER].push(ctrl1);

		var ctrl2 = document.createElement('div');
		ctrl2.style.cssText = style;
		ctrl2.innerHTML = '<input type="text" name="map-region" id="map-region" placeholder="在该区域中搜索, 例如: 成都市" style="width: 200px;" />';
		ctrl2.index = 2;
		ctrl2.style.margin = '0 5px 0 0';
		_map.controls[qq.maps.ControlPosition.TOP_CENTER].push(ctrl2);

		var ctrl3 = document.createElement('div');
		ctrl3.style.cssText = style;
		ctrl3.innerHTML = '<input type="button" value="搜索" />';
		ctrl3.index = 4;
		ctrl3.style.margin = '0 5px 0 0';
		qq.maps.event.addDomListener(ctrl3, 'click', _doSearch);
		_map.controls[qq.maps.ControlPosition.TOP_CENTER].push(ctrl3);
		
		// 如果有默认值
		if (initPointLat && initPointLng) {
			var pos = new qq.maps.LatLng(initPointLat, initPointLng);
			var mk = _buildMarker(_map, pos);

			_markers.push(mk);
			_selectMarker(mk);

			qq.maps.event.addListener(mk, 'click', function(event) {
				_selectMarker(event.target);
			});
		}

		return {
			GetPosition: function() {
				return _getPosition();
			}
		};
	}
	
	var newid1 = GenerateUniqueId('modal');
	var newid2 = GenerateUniqueId('map');
	var html = [
		'<div class="modal fade" id="'+newid1+'" tabindex="-1" role="dialog">',
			'<div class="modal-dialog modal-lg">',
				'<div class="modal-content">',
					'<div class="modal-header">',
						'<button type="button" class="close" data-dismiss="modal" aria-label="Close"><span aria-hidden="true">&times;</span></button>',
						'<h4 class="modal-title"><i class="fa fa-map-marker"></i> 地图选点</h4>',
					'</div>',
					'<div class="modal-body" style="padding:0;">',
						'<div id="'+newid2+'" style="width: 100%; height: 400px;"></div>',
					'</div>',
					'<div class="modal-footer">',
						'<span class="text-muted pull-left">输入地址来搜索，红色图标为选中的地址，若不准确，可拖动调整</span>',
						'<span class="text-danger error-msg" style="margin-right: 10px;"></span>',
						'<button type="button" class="btn btn-default" data-dismiss="modal">取消</button>',
						'<button type="button" class="btn btn-primary btn-confirm">确定选取</button>',
					'</div>',
				'</div>',
			'</div>',
		'</div>'
	].join('');

	$('body').append(html);
	var modal = $('#'+newid1);
	modal.on('hidden.bs.modal', function () { modal.remove(); });
	modal.on('shown.bs.modal', function () {
		var opt = {
			domId: newid2,
			initPointLat: options.initPointLat,
			initPointLng: options.initPointLng
		};
		
		var map = _QQMap(opt);

		modal.find('.btn-confirm').click(function() {
			modal.find('.error-msg').empty();
			var pos = map.GetPosition();
			if (pos == null) {
				modal.find('.error-msg').html('请先选取一个点！');
				return;
			}

			modal.modal('hide');
			if (typeof options.callback == 'function') {
				options.callback(pos);
			}
		});
	});
	modal.modal('show');
}

// 弹出层选择地址，用于一个页面需要选取多个地址的情况
// 例如：赠品投票的'可投票地区'和'不可投票地区'
function AreaModalSelector(callback) {
	var _showModal = function(html) {
		$('body').append(html);
		var modal = $('#AreaModalSelectorModalID');
		modal.on('shown.bs.modal', function() {
			var _areaSelector = InitAreaSelector(modal.find('.area-selector'));

			modal.find('.btn-confirm').click(function() {
				var areaId = _areaSelector.GetData();
				if (areaId) {
					$.ajax({
						type: 'POST',
						dataType: 'json',
						url: '/CommonService/GetAreaInfo/'.toLowerCase(),
						data: { areaId: areaId },
						success: function (data) {
							if (!data.error) {
								callback(data.data.Area);
								modal.modal('hide');
							}
						}
					});
				} else {
					IBBAlert.Error('请选择一个地区！');
				}
			});
		});
		modal.on('hidden.bs.modal', function() { modal.remove(); });
		modal.modal('show');
	};

	if (window.AreaModalSelectorHtml123) {
		_showModal(window.AreaModalSelectorHtml123);
	} else {
		$.ajax({
			type: 'POST',
			dataType: 'json',
			url: '/CommonService/GetAreaModalSelectorHtml/'.toLowerCase(),
			data: {},
			success: function(data) {
				if (!data.error) {
					window.AreaModalSelectorHtml123 = data.data.html;
					_showModal(window.AreaModalSelectorHtml123);
				}
			}
		});
	}
}

/**
 * 将数值四舍五入(保留2位小数)后格式化成金额形式
 *
 * @param num 数值(Number或者String)
 * @return 金额格式的字符串,如'1,234,567.45'
 * @type String
 */
function formatCurrency(num) {
    num = num.toString().replace(/\$|\,/g, '');
    if (isNaN(num))
        num = "0";
    sign = (num == (num = Math.abs(num)));
    num = Math.floor(num * 100 + 0.50000000001);
    cents = num % 100;
    num = Math.floor(num / 100).toString();
    if (cents < 10)
        cents = "0" + cents;
    for (var i = 0; i < Math.floor((num.length - (1 + i)) / 3) ; i++)
        num = num.substring(0, num.length - (4 * i + 3)) + ',' +
        num.substring(num.length - (4 * i + 3));
    return (((sign) ? '' : '-') + num + '.' + cents);
}

function ConvertToFloat(str) {
	if (!str) {
		return 0;
	}
	str = $.trim(str);
	if (!str || isNaN(str)) {
		return 0;
	}
	return parseFloat(str);
}

function ConvertToInt(str) {
	if (!str) {
		return 0;
	}
	str = $.trim(str);
	if (!str || isNaN(str)) {
		return 0;
	}
	return parseInt(str);
}

//// 一下方法用来格式xml字符串
function formatXml(text) {
    //去掉多余的空格  
    text = '\n' + text.replace(/(<\w+)(\s.*?>)/g, function ($0, name, props) {
        return name + ' ' + props.replace(/\s+(\w+=)/g, " $1");
    }).replace(/>\s*?</g, ">\n<");

    //把注释编码  
    text = text.replace(/\n/g, '\r').replace(/<!--(.+?)-->/g,
            function ($0, text) {
                var ret = '<!--' + escape(text) + '-->';
                //alert(ret);  
                return ret;
            }).replace(/\r/g, '\n');

    //调整格式  
    var rgx = /\n(<(([^\?]).+?)(?:\s|\s*?>|\s*?(\/)>)(?:.*?(?:(?:(\/)>)|(?:<(\/)\2>)))?)/mg;
    var nodeStack = [];
    var output = text.replace(rgx, function ($0, all, name, isBegin,
            isCloseFull1, isCloseFull2, isFull1, isFull2) {
        var isClosed = (isCloseFull1 == '/') || (isCloseFull2 == '/')
                || (isFull1 == '/') || (isFull2 == '/');
        //alert([all,isClosed].join('='));  
        var prefix = '';
        if (isBegin == '!') {
            prefix = getPrefix(nodeStack.length);
        } else {
            if (isBegin != '/') {
                prefix = getPrefix(nodeStack.length);
                if (!isClosed) {
                    nodeStack.push(name);
                }
            } else {
                nodeStack.pop();
                prefix = getPrefix(nodeStack.length);
            }

        }
        var ret = '\n' + prefix + all;
        return ret;
    });

    var prefixSpace = -1;
    var outputText = output.substring(1);
    //alert(outputText);  

    //把注释还原并解码，调格式  
    outputText = outputText.replace(/\n/g, '\r').replace(
            /(\s*)<!--(.+?)-->/g,
            function ($0, prefix, text) {
                //alert(['[',prefix,']=',prefix.length].join(''));  
                if (prefix.charAt(0) == '\r')
                    prefix = prefix.substring(1);
                text = unescape(text).replace(/\r/g, '\n');
                var ret = '\n' + prefix + '<!--'
                        + text.replace(/^\s*/mg, prefix) + '-->';
                //alert(ret);  
                return ret;
            });

    return outputText.replace(/\s+$/g, '').replace(/\r/g, '\r\n');

}

function getPrefix(prefixIndex) {
    var span = '    ';
    var output = [];
    for (var i = 0; i < prefixIndex; ++i) {
        output.push(span);
    }

    return output.join('');
}

/* 格式化JSON源码(对象转换为JSON文本) */
function formatJson(txt, compress) { /*compress：是否为压缩模式*/
    var indentChar = '    ';
    if (/^\s*$/.test(txt)) {
        //alert('数据为空,无法格式化! ');
        return txt;
    }
    try { var data = eval('(' + txt + ')'); }
    catch (e) {
        // alert('数据源语法错误,格式化失败! 错误信息: ' + e.description, 'err');
        return txt;
    };
    var draw = [], last = false, This = this, line = compress ? '' : '\n', nodeCount = 0, maxDepth = 0;

    var notify = function (name, value, isLast, indent/*缩进*/, formObj) {
        nodeCount++;/*节点计数*/
        for (var i = 0, tab = ''; i < indent; i++) tab += indentChar;/* 缩进HTML */
        tab = compress ? '' : tab;/*压缩模式忽略缩进*/
        maxDepth = ++indent;/*缩进递增并记录*/
        if (value && value.constructor == Array) {/*处理数组*/
            draw.push(tab + (formObj ? ('"' + name + '":') : '') + '[' + line);/*缩进'[' 然后换行*/
            for (var i = 0; i < value.length; i++)
                notify(i, value[i], i == value.length - 1, indent, false);
            draw.push(tab + ']' + (isLast ? line : (',' + line)));/*缩进']'换行,若非尾元素则添加逗号*/
        } else if (value && typeof value == 'object') {/*处理对象*/
            draw.push(tab + (formObj ? ('"' + name + '":') : '') + '{' + line);/*缩进'{' 然后换行*/
            var len = 0, i = 0;
            for (var key in value) len++;
            for (var key in value) notify(key, value[key], ++i == len, indent, true);
            draw.push(tab + '}' + (isLast ? line : (',' + line)));/*缩进'}'换行,若非尾元素则添加逗号*/
        } else {
            if (typeof value == 'string') value = '"' + value + '"';
            draw.push(tab + (formObj ? ('"' + name + '":') : '') + value + (isLast ? '' : ',') + line);
        };
    };
    var isLast = true, indent = 0;
    notify('', data, isLast, indent, false);
    return draw.join('');
}

/*
 * 前台直接上传到Aliyun OSS，可用于大文件的上传
 * (一般上传是上传到网站后台，再后台上传到Aliyun OSS)
 */
function OssDirectUploader(options) {
	var defaults = {
		container: null,
		folder: '',
		fileLimited: 10000,
		itemBuilderFunc: null,
		uploadSuccessCallback: null,
		deleteCallback: null
	};
	var settings = $.extend({}, defaults, options);
	var container = settings.container;

	if (!container) { return; }
	if (settings.fileLimited < 1) { settings.fileLimited = 1; }

	var accessId = container.find('input.accessId').val();
	var endpoint = container.find('input.endpoint').val();
	var policy = container.find('input.policy').val();
	var signature = container.find('input.signature').val();

	var filePicker = container.find('.file-picker');
	var fileList = container.find('.file-list');

	var opts = {
		pick: {
			id: filePicker,
			innerHTML: '选择文件',
			multiple: settings.fileLimited > 1
		},
		formData: {
			OSSAccessKeyId: accessId,
			policy: policy,
			Signature: signature
		},
		server: endpoint,
		compress: false,
		auto: true,
		duplicate: true,
		fileVal: 'file',
		disableGlobalDnd: true,
		fileSingleSizeLimit: 100 * 1024 * 1024,  // 100 M
		fileNumLimit: settings.fileLimited
	};
	var uploader = WebUploader.create(opts);
	filePicker.addClass('btn btn-primary btn-sm');
	_hideOrShowUploadBtn();

	container.on('click', '.btn-remove', function () {
		var item = $(this).closest('.item');
		_delete(item);
	});
	// 添加时
	uploader.on('fileQueued', function(file) {
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
	// 上传前
	uploader.on('uploadBeforeSend', function(block, data) {
		var file = block.file;
		// 修改文件名
		var key = _buildPhotoKey(settings.folder, file);
		data.key = key;
		
		// 记下文件名
		var item = fileList.find('.' + file.id);
		item.data('key', key);
	});
	// 上传时
	uploader.on('uploadProgress', function(file, percentage) {
		var item = fileList.find('.' + file.id);
		var bar = item.find('.progress .progress-bar');
		bar.css('width', percentage * 100 + '%');
	});
	// 上传成功
	uploader.on('uploadSuccess', function(file, response) {
		var item = fileList.find('.' + file.id);
		item.find('.progress').remove();

		if (!response.error) {
			var oriUrl = endpoint + '/' + item.data('key');
			item.find('.file-name').append('<a href="'+oriUrl+'" target="_blank"><i class="fa fa-file-text"></i> '+file.name+'</a>');
			item.data('key', item.key);
			item.data('name', file.name);
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
			text = '只能上传文件！';
		} else if (code == 'Q_EXCEED_NUM_LIMIT') {
			text = '抱歉，最多只能上传' + settings.fileLimited + '张图片！';
		} else if (code == 'Q_EXCEED_SIZE_LIMIT') {
			text = '抱歉，上传的文件太大了！';
		} else if (code == 'F_EXCEED_SIZE') {
			text = '抱歉，上传的文件太大了！';
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
		},
		GetDataWithFileName: function () {
		    var list = [];
		    fileList.find('.item').each(function () {
		        list.push({ key: $(this).data('key'), name: $(this).data('name') });
		    });
		    return list;
		},
		ClearData: function() {
			fileList.find('.item').each(function () {
				_delete($(this));
			});
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
		if (typeof settings.deleteCallback == 'function') {
			settings.deleteCallback(item);
		} else {
			item.remove();
			if (item.attr('id')) {
				uploader.removeFile(item.attr('id'), true);
			}
			_hideOrShowUploadBtn();
		}
	}

	function _buildPhotoKey(folder, file) {
		var d = new Date();
		var year = d.getFullYear();
		var month = d.getMonth() + 1;
		var day = d.getDate();

		if (month < 10) {
			month = '0' + month;
		}
		if (day < 10) {
			day = '0' + day;
		}
		var key = folder + '/' + year + '/' + month + '/' + day + '/' + uuid.v4() + '.' + file.ext;
		key = key.toLowerCase();
		return key;
	}
}

function DownloadByFormQuery(formId, url) {
    var form = $('#' + formId);
    var original = form.attr('action');
    form.attr('action', url);
    form.attr('target', '_blank');
    form.submit();
    form.attr('action', original);
    form.attr('target', '_self');
}

/*
 * 选择权限，
 * callback会传入参数 { Text: xxx, Value: yyy }
 */
function PermissionSelector(options) {
	var defaults = {
		multiple: false,
		callback: null
	};
	var settings = $.extend({}, defaults, options);
	var multiple = settings.multiple;
	var topCallback = settings.callback;
	if (typeof topCallback != 'function') {
		topCallback = function() {};
	}

	var url1 = '/Views/StaffUser/Partial/PermissionSelector.cshtml'.toLowerCase();
	var url2 = ('/commonservice/loadpartialviewform/?viewpath=' + encodeURIComponent(url1)).toLowerCase();
	IBBAlert.LoadRemoteModal(url1, { multiple: multiple }, function(modal) {
		var iptKeyword = modal.find('.ipt-keyword');
		var permissionWrap = modal.find('.permission-wrap');
		
		var _searchCallback = function(data) {
			permissionWrap.find('.permission-list').replaceWith(data.html);
		}

		var _mySearch = new ISearch(url2, _searchCallback);
		
		var _research = function() {
			var keyword = $.trim(iptKeyword.val());
			_mySearch.Parameters = { isSearch: true, multiple: multiple };
			_mySearch.Search(keyword);
		}

		iptKeyword.focus();
		iptKeyword.bind('input propertychange', function() {
			_research();
		});

		modal.find('.btn-confirm').click(function() {
			var plist = [];
			permissionWrap.find('.permission-list input.chk-permission:checked').each(function() {
				plist.push({
					Text: $(this).data('text'),
					Value: $(this).val()
				});
			});

			if (!plist || plist.length <= 0) {
				IBBAlert.Error('请先选择权限！');
			} else {
				modal.modal('hide');
				if (multiple) {
					topCallback(plist);
				} else {
					topCallback(plist[0]);
				}
			}
		});
	});
}

/*
 * 根据条码选择商品(功能参考：未知包裹收货)
 * parameters => 传到后台的额外参数
 * callback => 回调方法，会传入参数 product object
 */
function SelectProductByBarCode(options) {
	var defaults = {
		parameters: {},
		callback: null
	};
	var settings = $.extend({}, defaults, options);

	var topParameters = settings.parameters;
	var topCallback = settings.callback;
	if (!topParameters) {
		topParameters = {};
	}
	if (typeof topCallback != 'function') {
		topCallback = function() {};
	}

	var _closeModal = function(modal, product) {
		modal.modal('hide');
		topCallback(product);
	}
	//根据条行码输入框添加商品到列表
	var _addProductByBarCode = function(modal, ipt) {
		var barcode = $.trim(ipt.val());
		if (!barcode) { return; }

		var dd = $.extend({}, topParameters, { barcode: barcode });

		var msgWrap = modal.find('.msg-wrap');
		msgWrap.html('<span class="text-note">搜索中...</span>');

		$.ajax({
			type: 'POST',
			dataType: 'JSON',
			url: '/Product/Search/'.toLowerCase(),
			data: dd,
			success: function(data) {
				msgWrap.empty();
				if (!data.error) {
					ipt.val('').focus();

					if (data.data.list && data.data.list.length > 0) {
						_closeModal(modal, data.data.list[0]);
					} else {
						msgWrap.html('<span class="text-red">根据条形码找不到商品！</span>');
					}
				}
			},
			error: function() {
				msgWrap.empty();
				ipt.focus();
			}
		});
	}
	var _onModalShow = function(modal) {
		var iptBarCode = modal.find('.ipt-barcode');
		var btnConfirmBarCode = modal.find('.btn-barcode-confirm');
		var btnSelectProduct = modal.find('.btn-add-product');

		iptBarCode.focus();

		iptBarCode.bind('keyup', function(event) {
			var evt = document.all ? window.event : event;
			if ((evt.keyCode || evt.which) == 13) {
				_addProductByBarCode(modal, iptBarCode);
			}
		});

		btnConfirmBarCode.click(function() { _addProductByBarCode(modal, iptBarCode); });

		btnSelectProduct.click(function() {
			IBBProductSelector.Select({
				multiple: false,
				callback: function(product) {
					_closeModal(modal, product);
				},
				parameters: topParameters
			});
		});
	}

	var mhtml = [
		'<div class="modal fade" tabindex="-1" role="dialog">',
			'<div class="modal-dialog">',
				'<div class="modal-content">',
					'<div class="modal-header">',
						'<button type="button" class="close" data-dismiss="modal" aria-label="Close"><span aria-hidden="true">&times;</span></button>',
						'<h4 class="modal-title"><i class="fa fa-search"></i> 根据条码选择商品</h4>',
					'</div>',
					'<div class="modal-body">',
						'<div class="form-horizontal">',
							'<div class="form-group">',
								'<label class="col-sm-1 control-label"></label>',
								'<div class="col-sm-6">',
									'<input type="text" class="form-control ipt-barcode" placeholder="扫描/输入商品条码来选择商品">',
									'<div class="msg-wrap" style="height:25px; margin-top:3px;"></div>',
								'</div>',
								'<div class="col-sm-2">',
									'<button type="button" class="btn btn-primary btn-barcode-confirm">确定</button>',
								'</div>',
								'<div class="col-sm-3">',
									'<button type="button" class="btn btn-default btn-add-product">手动选择商品</button>',
								'</div>',
							'</div>',
						'</div>',
					'</div>',
					'<div class="modal-footer">',
						'<button type="button" class="btn btn-default" data-dismiss="modal">关闭</button>',
					'</div>',
				'</div>',
			'</div>',
		'</div>'
	].join('');

	var modal = $(mhtml);
	$('body').append(modal);
	modal.on('shown.bs.modal', function() { _onModalShow(modal); });
	modal.on('hidden.bs.modal', function() { modal.remove(); });
	modal.modal('show');
}

// 弹出层选择代理商
function IBBSalesAgentSelector2(options) {
	var defaults = {
		parameters: {},
		callback: null
	};
	var settings = $.extend({}, defaults, options);
	
	var topParameters = settings.parameters;
	var topCallback = settings.callback;
	
	if (!topParameters) { topParameters = {}; }
	if (typeof topCallback != 'function') { topCallback = function() {}; }

	var _searchUrl = '/CommonService/SearchSalesAgent2/'.toLowerCase();
	var topModal = null;
	var _areaSelector = null;
	var _resultList = [];

	var _replaceTpl = function(tpl, item) {
		return tpl.replace(/\{Id\}/g, item.Id)
				  .replace(/\{IdNumber\}/g, item.IdNumber)
				  .replace(/\{StoreName\}/g, item.StoreName)
				  .replace(/\{SalesAgentLevel\}/g, item.SalesAgentLevel)
				  .replace(/\{SalesAgentLevelDesc\}/g, item.SalesAgentLevelDesc)
				  .replace(/\{Mobile\}/g, item.Mobile)
				  .replace(/\{FuzzyMobile\}/g, item.FuzzyMobile);
	};
	var _searchSuccessCallback = function(data) {
		var rstxt = '共有 <span class="text-primary">'+data.TotalCount+'</span> 条搜索结果';
		topModal.find('.result-text').html(rstxt);

		var list1 = topModal.find('.result-list');
		var tpl = topModal.find('.item-tpl-wrap1').html();
		list1.show().empty();
		
		if (data.List && data.List.length > 0) {
			for (var i = 0; i < data.List.length; i++) {
				var item = data.List[i];
				_resultList.push(item);

				var html = _replaceTpl(tpl, item);
				list1.append(html);
			}
		}

		var items = list1.find('.item');
		items.off('click');
		items.on('click', function() {
			$(this).toggleClass('selected');
		});
	};

	var _mySearch = new ISearch(_searchUrl, _searchSuccessCallback);
	
	var _research = function() {
		// 代理商编号或名称
		var keyword = $.trim(topModal.find('.ipt-name').val());

		// 代理商等级
		var ll = [];
		var levelFilter = topModal.find('.level-filter');
		levelFilter.find('input[name="chk-level"]:checked').each(function() {
			ll.push($(this).val());
		});
		topParameters.levelList = ll.join(',');

		// 代理商状态
		var ss = [];
		var statusFilter = topModal.find('.status-filter');
		statusFilter.find('input[name="chk-status"]:checked').each(function() {
			ss.push($(this).val());
		});
		topParameters.statusList = ss.join(',');

		// 是否有上级代理商
		var upperAgentFilter = topModal.find('.upper-agent-filter');
		topParameters.hasUpperAgent = upperAgentFilter.find('input.radio-upperAgent:checked').val();

		// 所属地区
		topParameters.areaId = _areaSelector == null ? '' : _areaSelector.GetData();

		topModal.find('.result-text').html('搜索中...');

		_mySearch.Parameters = topParameters;
		_mySearch.Search(keyword);
	};
	var _closeModal = function() {
		var returnList = [];
		for (var i = 0; i < _resultList.length; i++) {
			var rs = _resultList[i];
			var selected = topModal.find('.result-list .item[data-id="'+rs.Id+'"]').hasClass('selected');
			if (selected) {
				returnList.push(rs);
			}
		}
		if (returnList && returnList.length > 0) {
			topModal.modal('hide');
			topCallback(returnList);
		} else {
			IBBAlert.Error('请选择代理商！');
		}
	}
	var _showModal = function(modal) {
		topModal = modal;

		// 代理商编号或名称
		topModal.find('.ipt-name').focus();
		topModal.find('.ipt-name').bind('input propertychange', function() {
			_research();
		});

		// 代理商等级
		var levelFilter = topModal.find('.level-filter');
		levelFilter.find('input[name="chk-level"]').change(function() {
			_research();
		});

		// 代理商状态
		var statusFilter = topModal.find('.status-filter');
		statusFilter.find('input[name="chk-status"]').change(function() {
			_research();
		});

		// 是否有上级代理商
		var upperAgentFilter = topModal.find('.upper-agent-filter');
		upperAgentFilter.find('input.radio-upperAgent').change(function() {
			_research();
		});

		// 所属地区
		var areaFilter = topModal.find('.area-filter');
		_areaSelector = InitAreaSelector(areaFilter.find('.area-selector'));
		areaFilter.on('change', 'select', _research);

		topModal.find('.btn-select-all').click(function() {
			topModal.find('.result-list .item').addClass('selected');
		});

		topModal.find('.btn-confirm').click(function() {
			_closeModal();
		});
	}
	IBBAlert.LoadRemoteModal('/Views/BaoMa/Partial/SalesAgentSearchModal2.cshtml', null, _showModal);
}

function ShowChoosePriceTemplateForm(forPerInventory, callback, selectedTemplateId) {
    IBBAlert.LoadRemoteModal('/Views/Product/Partial/ChoosePriceTemplateForm.cshtml', { forPerInventory: forPerInventory, selectedTemplateId: selectedTemplateId }, function (modal) {
        modal.find('.result-list .item').click(function () {
            modal.find('.result-list .selected').removeClass('selected');
            modal.find('.result-list .select-icon').each(function () { $(this).html('<i class="fa fa-circle"></i>'); });
            $(this).addClass('selected');
            $(this).find('.select-icon').html('<i class="fa fa-check-circle"></i>');
        });
        modal.find('#chooseTemplateBtn').click(function () {
            var s = modal.find('.result-list .selected');
            if (s.length <= 0) {
                IBBAlert.Error('请先选择一个价格模板');
                return;
            }
            var obj = {};
            obj.Id = s.data('id');
            obj.Name = s.data('name');
            modal.modal('hide');
            if (typeof callback == 'function') {
                callback(obj);
            }
        });
    });
}

// 批量设置商品的上下架状态
function BulkSetProductDisplayAndSale(productIds) {
	if (!productIds || productIds.length <= 0) {
		IBBAlert.Error('请选择要上下架的商品！');
		return;
	}

	var _getDatas = function(modal) {
		var datas = [];
		modal.find('.status-table tbody tr').each(function() {
			var tr = $(this);
			datas.push({
				channelId: parseInt(tr.data('channel')),
				displayable: tr.find('.chk-displayable').prop('checked'),
				saleable: tr.find('.chk-saleable').prop('checked')
			});
		});
		return datas;
	}

	var _showModal = function(modal) {
		modal.find('.btn-select-all').click(function() {
			modal.find('.status-table input[type="checkbox"]').prop('checked', true);
		});
		modal.find('.btn-unselect-all').click(function() {
			modal.find('.status-table input[type="checkbox"]').prop('checked', false);
		});
		
		modal.find('.btn-confirm').click(function() {
			var btn = $(this);

			var dd = _getDatas(modal);
			
			btn.button('loading');
			$.ajax({
				type: 'POST',
				dataType: 'json',
				url: modal.find('.iptSubmitUrl').val(),
				data: {
					ids: modal.find('.iptProductIds').val(),
					datas: JSON.stringify(dd)
				},
				success: function(data) {
					btn.button('reset');
					if (!data.error) {
						modal.modal('hide');
						toastr.success('设置成功！');
					}
				},
				error: function() {
					btn.button('reset');
				}
			});
		});
	}

	IBBAlert.LoadRemoteModal('/Views/Product/Partial/BulkSetDisplayAndSaleModal.cshtml', { ids: productIds.join(',') }, _showModal);
}

// 单独启用/禁用用户级别的自助提现
function SetUserBalanceAllowSelfWithdraw(userId, allowSelfWithdraw) {
	if (!userId || typeof allowSelfWithdraw != 'boolean') {
		return;
	}
	var msg = '确认禁用消费者的自助提现功能？<br />禁用后，消费者用户将不能从前台发起提现申请。';
	if (allowSelfWithdraw) {
		msg = '确认启用消费者的自助提现功能？<br />启用后，消费者用户将能够从前台发起提现申请。';
	}
	IBBAlert.Confirm(msg, function(yes) {
		if (yes) {
			IBBAlert.ShowLoadingLayer();
			$.ajax({
				type: 'POST',
				dataType: 'json',
				url: '/Balance/AjaxSetAllowSelfWithdraw/'.toLowerCase(),
				data: {
					userId: userId,
					allowSelfWithdraw: allowSelfWithdraw
				},
				success: function(data) {
					IBBAlert.ShowLoadingLayer(true);
					if (!data.error) {
						toastr.success('设置成功！');
						window.location.reload();
					}
				},
				error: function() {
					IBBAlert.ShowLoadingLayer(true);
				}
			});
		}
	});
}
//验证Guid形式
function CheckIsGuid(testID) {
    var reg = new RegExp(/^[0-9a-z]{8}-[0-9a-z]{4}-[0-9a-z]{4}-[0-9a-z]{4}-[0-9a-z]{12}$/);
    if (reg.test(testID)) {
        return true;
    }
    return false;
}
function TableToggleInit() {
    $(".body-toggle").click(function () {
                    $(this).parent().parent().next().toggle();

                    if ($(this).hasClass("fa-plus-circle")) {
                        $(this).removeClass("fa-plus-circle").addClass("fa-minus-circle");
                    } else {
                        $(this).removeClass("fa-minus-circle").addClass("fa-plus-circle");
                    }
                });
}


function CheckSocketMsgKey(socketMsg,msgKey) {
    var index = socketMsg.indexOf("msgKey:" + msgKey);
    if (index >= 0)
        return true;
    else
        return false;
}
function GetSocketMsgBody(socketMsg) {
    var arr = socketMsg.split("msgbody:");
    if (arr && arr.length === 2)
        return arr[1];
    else
        return "";
}
function HandleSocketMsgBodyWithMsgKey(socketMsg, msgKey,callback) {
    if (CheckSocketMsgKey(socketMsg, msgKey)) {
        var msgBody = GetSocketMsgBody(socketMsg);
        if (msgBody !== "") {
            callback(msgBody);
        }
    }
}