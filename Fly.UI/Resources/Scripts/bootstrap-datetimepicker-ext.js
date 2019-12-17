$(function () {
	var dtp = {
		init: function (selector) {
			selector = selector || '.dtp-date';

			$(selector).each(function () {
				var me = $(this);

				var opt = {
					locale: 'zh-cn',
					sideBySide: true,
					useCurrent: false,
					format: me.data('format'),
					defaultDate: me.data('defaultdate')
				};

				var minDate1 = me.data('mindate');
				if (minDate1) {
					opt.minDate = minDate1;
				}
				var maxDate2 = me.data('maxdate');
				if (maxDate2) {
					opt.maxDate = maxDate2;
				}
				me.datetimepicker(opt);

				var idFormat = '#{id}-dtp-container';
				var picker = me.data('DateTimePicker');

				var notBefore = me.data('notbefore');
				if (notBefore) {
					var id = idFormat.replace('{id}', notBefore);
					var d1 = $(id).data('defaultdate');
					if (d1) {
						picker.minDate(d1);
					}

					$(id).on('dp.change', function (event) {
						picker.minDate(event.date);
					});
				}

				var notAfter = me.data('notafter');
				if (notAfter) {
					var id = idFormat.replace('{id}', notAfter);
					var d1 = $(id).data('defaultdate');
					if (d1) {
						picker.maxDate(d1);
					}
					
					$(id).on('dp.change', function (event) {
						picker.maxDate(event.date);
					});
				}

				me.find('input').focus(function () {
					picker.show();
				});
			});
		}
	};
	dtp.init();

	window.DateTimePickerInit = dtp.init;
});