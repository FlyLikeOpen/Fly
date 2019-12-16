$(function () {
	var drp = {
		init: function(selector) {
			selector = selector || '.date-range-picker';
			$(selector).each(function () {
				var me = $(this);
				var allowEmpty = me.data('allowempty') == '1';
				var vformat = me.data('format');
				var fromId = me.data('fromid');
				var toId = me.data('toid');
				var errorMsg = me.data('errormsg');
				var option = me.data('option');
				var ranges = me.data('ranges');

				var rans = {};
				$(ranges).each(function(key, val) {
					rans[val[0]] = [ moment(val[1]), moment(val[2]) ];
				});
				option.ranges = rans;
				
				if (option.startDate) { option.startDate = moment(option.startDate); }
				if (option.endDate) { option.endDate = moment(option.endDate); }
				if (option.minDate) { option.minDate = moment(option.minDate); }
				if (option.maxDate) { option.maxDate = moment(option.maxDate); }

				var valueLabel = me.find('.value-label');

				me.daterangepicker(option, function (start, end, label) {
					if (start.unix() > end.unix()) {
						IBBAlert.Error(errorMsg);
						return;
					}
					var s1 = start.format(vformat);
					var s2 = end.format(vformat);
					$('#' + fromId).val(s1);
					$('#' + toId).val(s2);
					var val = s1 + ' 至 ' + s2;
					valueLabel.html(val);
				});

				if (allowEmpty) {
					me.on('cancel.daterangepicker', function (ev, picker) {
						$('#' + fromId).val('');
						$('#' + toId).val('');
						valueLabel.html('');
					});
				}
			});
		}
	};
	drp.init();
});