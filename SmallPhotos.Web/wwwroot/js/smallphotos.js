function smpInitialise() {
    $(window).resize(function() {
        $('aside').css('display', '');
        if ($('.nav-close:visible').length > 0) {
            $('.nav-close').css('display', '');
            $('.nav-show').css('display', '');
        }
    });
    $('.nav-show').click(function() {
        $('aside').fadeToggle('fast');
        $(this).hide();
        $('.nav-close').show();
    });
    $('.nav-close').click(function() {
        $('aside').hide();
        $(this).hide();
        $('.nav-show').show();
    });
    $('[data-href]').click(function(e) {
        window.location.href = $(this).attr('data-href');
        e.preventDefault();
        return false;
    });
    $('[data-depends]').each(function() {
        let btnWithDependency = $(this);
        let dependentFormObject = $(btnWithDependency.attr('data-depends'));
        if (dependentFormObject.is('input')) {
            dependentFormObject.on('keypress', function(e) {
                if (btnWithDependency.attr('disabled') && (e.keyCode || e.which) === 13) {
                    e.preventDefault();
                    return false;
                }
            });
        }
        dependentFormObject.on('change input paste keyup pickmeup-change', function() {
            let dependentValue = $(this).val();
            btnWithDependency.prop('disabled', dependentValue === null || dependentValue.match(/^\s*$/) !== null);
        });
        dependentFormObject.trigger('change');
    });

    $('[data-confirm]').click(function(event) {
        if (!confirm($(this).attr('data-confirm'))) {
            event.preventDefault();
            return false;
        }
    });
}

function fullSizeImage() {
    var win = $(window);
    var fullImg = $('#fullimg');
    var isFullImgScaled = $('#fullimg-scaled').prop('checked');
    if (isFullImgScaled) {
        var isCompact = win.width() <= 600;
        var height = win.height() - (isCompact ? 340 : 290);
        var width = win.width() - (isCompact ? 30 : 280);
        console.log('scaling image size to ' + width + 'x' + height);

        fullImg.css('max-height', height);
        fullImg.css('max-width', width);
    } else {
        console.log('disabled image scaling, removing max height/width css');
        fullImg.css('max-height', '');
        fullImg.css('max-width', '');
    }
}