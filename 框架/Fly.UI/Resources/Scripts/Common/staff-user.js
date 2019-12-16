$(document).ready(function () {
    var adminUserQueryPage = $('#p-StaffUser-query');
    if (adminUserQueryPage.length > 0) {
        var page = adminUserQueryPage;

        var setPermission = function (text, value) {
            page.find('.ipt-perText').val(text);
            page.find('.ipt-perValue').val(value);
        }

        page.find('.btn-select-permission').click(function () {
            PermissionSelector({
                multiple: false,
                callback: function (per) {
                    setPermission(per.Text, per.Value);
                }
            });
        });

        page.find('.btn-clear-permission').click(function () {
            setPermission('', '');
        });

        $('.set-valid-user').click(function () {
            var btn = $(this);
            var uid = btn.data('uid');
            var enable = btn.data('enable') == '1';
            var act = enable ? '启用' : '禁用';

            IBBAlert.Confirm('确认要' + act + '此账号么？', function (y) {
                if (y == false) {
                    return;
                }

                btn.button('loading');
                $.ajax({
                    type: 'POST',
                    dataType: 'json',
                    url: $('#iptSetValidUrl').val(),
                    data: { userId: uid, valid: enable },
                    success: function (data) {
                        btn.button('reset');
                        if (!data.error) {
                            toastr.success(act + '此账号成功！');
                            window.location.reload();
                        }
                    }
                });
            });
        });
    }

    var _saveUser2 = function (page, btn) {
        var rlist = [];
        page.find('.role-list .chk-role:checked').each(function () {
            var val = $(this).val();
            rlist.push(val);
        });

        btn.button('loading');
        $.ajax({
            type: 'POST',
            dataType: 'json',
            url: page.find('#Url_Submit').val(),
            data: {
                adminUserId: page.find('#iptStaffUserId').val(),
                userName: page.find('.ipt-userName').val(),
                displayName: page.find('.ipt-displayName').val(),
                password: page.find('.ipt-password').val(),
                password2: page.find('.ipt-password2').val(),
                email: page.find('.ipt-email').val(),
                mobile: page.find('.ipt-mobile').val(),
                roles: rlist.join(',')
            },
            success: function (data) {
                btn.button('reset');
                if (!data.error) {
                    toastr.success('即将刷新页面', '保存成功！');
                    window.location.href = data.data.url;
                }
            },
            error: function () {
                btn.button('reset');
            }
        });
    }

    var new2Page = $('#p-StaffUser-new2');
    if (new2Page.length > 0) {
        var page = new2Page;

        $('#btnSave').click(function () {
            _saveUser2(page, $(this));
        });
    }

    var edit2Page = $('#p-StaffUser-edit2');
    if (edit2Page.length > 0) {
        var page = edit2Page;

        $('#btnSave').click(function () {
            _saveUser2(page, $(this));
        });
    }

    var detailPage = $('#p-StaffUser-detail');
    if (detailPage.length > 0) {
        var page = detailPage;
        var userId = page.find('#iptStaffUserId').val();

        page.find('.temp-permission-table tr td .btn-remove').click(function () {
            var btn = $(this);
            var tr = btn.closest('tr');
            var key = tr.data('key');

            btn.button('loading');
            $.ajax({
                type: 'POST',
                dataType: 'json',
                url: page.find('#Url_DoRemoveTempPermission').val(),
                data: {
                    userId: userId,
                    key: key
                },
                success: function (data) {
                    btn.button('reset');
                    if (!data.error) {
                        toastr.success('即将刷新页面', '保存成功！');
                        tr.remove();
                    }
                },
                error: function () {
                    btn.button('reset');
                }
            });
        });
    }

	// 权限列表的交互
	var _initPermissionList = function(page) {
		var plist = page.find('.permission-list');

		plist.find('.sub-list').each(function() {
			var sublist = $(this);
			var list1 = sublist.find('.body .chk-permission');
			var list2 = sublist.find('.body .chk-permission:checked');
			var checked = list1.length == list2.length;
			sublist.find('.head .chk-permission').prop('checked', checked);
		});

		plist.find('.sub-list .head .chk-permission').change(function() {
			var sublist = $(this).closest('.sub-list');
			var checked = $(this).prop('checked');
			sublist.find('.body .chk-permission').prop('checked', checked);
		});

		plist.find('.sub-list .body .chk-permission').change(function() {
			var sublist = $(this).closest('.sub-list');
			var list1 = sublist.find('.body .chk-permission');
			var list2 = sublist.find('.body .chk-permission:checked');
			if (list1.length == list2.length) {
				sublist.find('.head .chk-permission').prop('checked', true);
			} else {
				sublist.find('.head .chk-permission').prop('checked', false);
			}
		});
	}

	// 新增/编辑角色公用
	var _initRole = function(page) {
		_initPermissionList(page);

		var modalHtml = page.find('.modal-html-holder').html();
		page.find('.modal-html-holder').remove();

		page.find('.btn-copy-permission').click(function() {
			var modal = $(modalHtml);
			$('body').append(modal);

			modal.on('hidden.bs.modal', function() {
				modal.remove();
			});

			modal.on('shown.bs.modal', function() {
				var sltRole = modal.find('select.slt-role');
				sltRole.selectpicker('render');

				modal.find('.btn-confirm').click(function() {
					var roleId = sltRole.val();
					if (roleId) {
						var url = page.find('#iptPageUrl').val();
						if (url.indexOf('?') > -1) {
							url += '&';
						} else {
							url += '?';
						}
						url += 'copyfrom=' + roleId;
						window.location.href = url;
					} else {
						IBBAlert.Error('请选择要导入的系统角色！');
					}
				});
			});
			
			modal.modal('show');
		});

		// 用户关键字过滤
		var iptUserKeyword = page.find('.ipt-userkeyword');
		iptUserKeyword.bind('input propertychange', function() {
			var keyword = $.trim(iptUserKeyword.val());
			page.find('.role-user-list .chk-user').each(function() {
				var chk = $(this)
				if (chk.val()) {
					var wrap = chk.closest('.checkbox');
					var name = chk.data('name');
					if (!keyword || name.indexOf(keyword) > -1) {
						wrap.show();
					} else {
						wrap.hide();
					}
				}
			});
		});

		// 权限关键字过滤
		var iptPerKeyword = page.find('.ipt-perkeyword');
		iptPerKeyword.bind('input propertychange', function() {
			var keyword = $.trim(iptPerKeyword.val());
			page.find('.permission-list .chk-permission').each(function() {
				var chk = $(this)
				if (chk.val()) {
					var wrap = chk.closest('.checkbox');
					var text = chk.data('text');
					if (!keyword || text.indexOf(keyword) > -1) {
						wrap.addClass('show1').removeClass('hide1').show();
					} else {
						wrap.removeClass('show1').addClass('hide1').hide();
					}
				}
			});

			page.find('.permission-list .sub-list').each(function() {
				var sublist = $(this);
				var l1 = sublist.find('.body .checkbox.show1').length;
				if (l1 == 0) {
					sublist.hide();
				} else {
					sublist.show();
				}
			});
		});
	}

	var _saveRole = function(page, btn) {
		var ulist = [];
		page.find('.role-user-list .chk-user:checked').each(function() {
			var val = $(this).val();
			ulist.push(val);
		});

		var plist = [];
		page.find('.permission-list .chk-permission:checked').each(function() {
			var val = $(this).val();
			plist.push(val);
		});

		btn.button('loading');
		$.ajax({
			type: 'POST',
			dataType: 'json',
			url: page.find('#Url_Submit').val(),
			data: {
				roleId: page.find('#iptRoleId').val(),
				roleName: page.find('.ipt-roleName').val(),
				roleDesc: page.find('.ipt-roleDesc').val(),
				users: ulist.join(','),
				permissions: plist.join(';')
			},
			success: function(data) {
				btn.button('reset');
				if (!data.error) {
					toastr.success('即将刷新页面', '保存成功！');
					window.location.href = data.data.url;
				}
			},
			error: function() {
				btn.button('reset');
			}
		});
	}

	// 新增角色
	var adminUserNewRolePage = $('#p-StaffUser-newrole');
	if (adminUserNewRolePage.length > 0) {
		var page = adminUserNewRolePage;
		
		_initRole(page);
		$('#btnSave').click(function() {
			_saveRole(page, $(this));
		});
	}

	// 编辑角色
	var adminUserEditRolePage = $('#p-StaffUser-editrole');
	if (adminUserEditRolePage.length > 0) {
		var page = adminUserEditRolePage;
		
		_initRole(page);
		$('#btnSave').click(function() {
			_saveRole(page, $(this));
		});
	}

	var adminUserQueryRolePage = $('#p-StaffUser-queryrole');
	if (adminUserQueryRolePage.length > 0) {
		var page = adminUserQueryRolePage;

		var setPermission = function(text, value) {
			page.find('.ipt-perText').val(text);
			page.find('.ipt-perValue').val(value);
		}

		page.find('.btn-select-permission').click(function() {
			PermissionSelector({
				multiple: false,
				callback: function(per) {
					setPermission(per.Text, per.Value);
				}
			});
		});

		page.find('.btn-clear-permission').click(function() {
			setPermission('', '');
		});
	}

	// 临时权限
	var adminUserSetTempPermissionPage = $('#p-StaffUser-SetTempPermission');
	if (adminUserSetTempPermissionPage.length > 0) {
		var page = adminUserSetTempPermissionPage;

		var longPermissions = JSON.parse(page.find('.json-long-permission').text());
		var tempPermissions = JSON.parse(page.find('.json-temp-permission').text());

		var buildPermissionItem = function(per) {
			var aa = '';
			if ($.inArray(per.Value, longPermissions) > -1) {
				aa = [
					' <span data-toggle="tooltip" data-placement="top" title="该权限已经是用户所属角色拥有的权限。">',
						'<span class="label label-success"><i class="fa fa-info-circle"></i> 角色权限</span>',
					'</span>'
				].join('');
			}

			var bb = '';
			for (var i = 0; i < tempPermissions.length; i++) {
				var temp = tempPermissions[i];
				if (temp.PermissionKey.toLowerCase() == per.Value.toLowerCase()) {
					var validText = temp.Valid ? '有效' : '无效';
					bb = [
						' <span data-toggle="tooltip" data-placement="top" title="用户已有该临时权限（当前'+validText+'）。保存将会更新该临时权限。">',
							'<span class="label label-info"><i class="fa fa-info-circle"></i> 临时权限</span>',
						'</span>'
					].join('');
				}
			}

			var item = [
				'<div class="permission" data-text="'+per.Text+'" data-value="'+per.Value+'">',
					per.Text + aa + bb,
					' <a class="text-danger btn-delete-perkey" href="javascript:;"><i class="fa fa-times-circle"></i></a>',
				'</div>'
			].join('');

			return item;
		}

		var tempTable = page.find('.temp-permission-table');
		var tpl = '<tr class="item">' + tempTable.find('tr.item-tpl').html() + '</tr>';
		page.find('#btnAddRow').click(function() {
			var tr = $(tpl);
			tr.find('input.radio-setType').attr('name', GenerateUniqueId('radio'));
			tempTable.find('tbody').append(tr);

			// 初始化控件
			tr.find('.positive-zeroint').each(function() {
				$(this).positiveZeroInt();
			});
			tr.find('.positive-int').each(function() {
				$(this).positiveInt();
			});
			window.DateTimePickerInit(tr.find('.dtp-date'));

			var perlist = tr.find('.per-list');
			tr.find('.btn-addperkey').click(function() {
				PermissionSelector({
					multiple: true,
					callback: function(pers) {
						for (var i = 0; i < pers.length; i++) {
							var per = pers[i];
							var html = buildPermissionItem(per);
							perlist.append(html);
							var td = tr.find('.col-perkey');
							td.find('span[data-toggle="tooltip"]').tooltip();
							td.find('.btn-delete-perkey').click(function() {
								$(this).closest('.permission').remove();
							});
						}
					}
				});
			});

			tr.find('.radio-setType').change(function() {
				var val = tr.find('.radio-setType:checked').val();
				if (val == 'FastSet') {
					tr.find('.col-fromto .fast-set-wrap').show();
					tr.find('.col-fromto .manual-set-wrap').hide();
				} else if (val == 'ManualSet') {
					tr.find('.col-fromto .fast-set-wrap').hide();
					tr.find('.col-fromto .manual-set-wrap').show();
				}
			});
			tr.find('.btn-remove').click(function() {
				$(this).closest('tr').remove();
			});
		});

		var getPermissionData = function() {
			var pplist = [];
			tempTable.find('tr.item').each(function() {
				var tr = $(this);
				var setType = tr.find('input.radio-setType:checked').val();
				var day = tr.find('.ipt-day').val();
				var hour = tr.find('.ipt-hour').val();
				var minute = tr.find('.ipt-minute').val();
				var from = tr.find('input[name="ipt-from"]').val();
				var to = tr.find('input[name="ipt-to"]').val();
				//var maxTimes = tr.find('.ipt-maxtimes').val();

				tr.find('.per-list .permission').each(function() {
					var text = $(this).data('text');
					var value = $(this).data('value');
					pplist.push({
						Text: text,
						Value: value,
						SetType: setType,
						Day: day,
						Hour: hour,
						Minute: minute,
						From: from,
						To: to//,
						//MaxTimes: maxTimes
					});
				});
			});

			return pplist;
		};

		$('#btnSave').click(function() {
			var btn = $(this);
			var list = getPermissionData();

			btn.button('loading');
			$.ajax({
				type: 'POST',
				dataType: 'json',
				url: page.find('#Url_Submit').val(),
				data: {
					userId: page.find('#iptStaffUserId').val(),
					json: JSON.stringify(list)
				},
				success: function(data) {
					btn.button('reset');
					if (!data.error) {
						toastr.success('即将返回到详情页', '保存成功！');
						window.location.href = data.data.url;
					}
				},
				error: function() {
					btn.button('reset');
				}
			});
		});
	}
});