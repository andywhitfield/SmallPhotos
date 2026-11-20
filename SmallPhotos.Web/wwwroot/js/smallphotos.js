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

    $('[data-starred]').each(function() {
        let star = $(this);

        star.click(function() {
            let photoId = star.attr('data-photoid');
            let isStarred = star.attr('data-starred').toLowerCase() === 'true';
            let uri = 'api/photoapi/' + (isStarred ? 'unstar' : 'star') + '/' + photoId;
            $.post(uri, {}, function() {
                star.attr('data-starred', isStarred ? 'false' : 'true');
                onDataStarredChange();
            });
        });

        onDataStarredChange();

        function onDataStarredChange() {
            if (star.attr('data-starred').toLowerCase() === 'true') {
                star.attr('title', 'Click to unstar this photo');
                star.addClass('starred');
            } else {
                star.attr('title', 'Click to star this photo');
                star.removeClass('starred');
            }
        }
    });
    const photosOnMapForm = $('form[name="photos-on-map"]');
    const photoIdsOnPage = $('[data-photoid]');
    if (photosOnMapForm.length === 1 && photoIdsOnPage.length) {
        console.log('showing the photos on a map link for '+photoIdsOnPage.length+' photo(s)');
        $('input[name="photoids"]', photosOnMapForm).val(
            photoIdsOnPage.map(function() {
                return $(this).attr('data-photoid');
            }).get().join());
        photosOnMapForm.css('display', 'inline');        
    }
    
    $('#filter-date-button').click(function() { $('span.filter-date-controls').toggle(); });
    const filterButton = $('#filter-date-apply');
    if (filterButton.length) {
        const fromDateSelector = new Datepicker((document.querySelector('input[name="fromdate"]')), {
            autohide: true,
            format: 'dd M yyyy',
            minDate: new Date(filterButton.attr('data-mindate')),
            maxDate: new Date(filterButton.attr('data-maxdate'))
        });
        const toDateSelector = new Datepicker((document.querySelector('input[name="todate"]')), {
            autohide: true,
            format: 'dd M yyyy',
            minDate: new Date(filterButton.attr('data-mindate')),
            maxDate: new Date(filterButton.attr('data-maxdate'))
        });
        filterButton.click(function() {
            if (fromDateSelector.getDate().getTime() > toDateSelector.getDate().getTime()) {
                alert('The "from date" cannot be after the "to date". Please select a valid date range.');
                return;
            }
            var newUrl = new URL(window.location.href);
            newUrl.searchParams.set('fromDate', fromDateSelector.getDate('yyyy-mm-dd'));
            newUrl.searchParams.set('toDate', toDateSelector.getDate('yyyy-mm-dd'));
            window.location.href = newUrl.toString();
        });
        if (filterButton.attr('data-filter-applied') === 'true')
            $('span.filter-date-controls').show();
    }

    $('.photo-select-dropdown-btn').on('click', onPhotoSelectMenuOpen);
    $('input.photo-select-checkbox[type="checkbox"]').on('click', onPhotoSelected);
    $('.photo-select-dropdown-content-select-all').on('click', photoSelectAll);
    $('.photo-select-dropdown-content-select-none').on('click', photoSelectNone);
    $('.photo-select-dropdown-content-star').on('click', photoSelectStar);
    $('.photo-select-dropdown-content-unstar').on('click', photoSelectUnstar);
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

function onPhotoSelectMenuOpen() {
    let selectCount = $('input.photo-select-checkbox[type="checkbox"]:checked').length;
    if (selectCount > 0) {
        console.debug('showing menu for selected photos');
        $('.photo-select-dropdown-content').show();
        return;
    }

    let photoSelector = $('.photo-select');
    if (photoSelector.hasClass('photo-select-visible'))
        photoSelector.removeClass('photo-select-visible');
    else
        photoSelector.addClass('photo-select-visible');
}

function onPhotoSelected() {
    let selectCount = $('input.photo-select-checkbox[type="checkbox"]:checked').length;
    let photoSelector = $('.photo-select');
    if (!photoSelector.hasClass('photo-select-visible'))
        photoSelector.addClass('photo-select-visible');
    
    $('button.photo-select-dropdown-btn').html('&#10003;' + (selectCount == 0 ? '' : (' ' + selectCount + ' selected')));
}

function photoSelectAll() {
    $('input.photo-select-checkbox[type="checkbox"]').prop('checked', true);
    onPhotoSelected();
}

function photoSelectNone() {
    $('input.photo-select-checkbox[type="checkbox"]').prop('checked', false);
    onPhotoSelected();
}

function photoSelectStar() {
    $('input.photo-select-checkbox[type="checkbox"]:checked').each(function() {
        let selectedPhotoId = $(this).attr('data-photoid');
        console.debug('setting photo ' + selectedPhotoId + ' as starred');
        $('[data-starred][data-photoid="'+selectedPhotoId+'"][data-starred="false" i]').trigger('click');
    });
}

function photoSelectUnstar() {
    $('input.photo-select-checkbox[type="checkbox"]:checked').each(function() {
        let selectedPhotoId = $(this).attr('data-photoid');
        console.debug('setting photo ' + selectedPhotoId + ' as unstarred');
        $('[data-starred][data-photoid="'+selectedPhotoId+'"][data-starred="true" i]').trigger('click');
    });
}

coerceToArrayBuffer = function (thing, name) {
    if (typeof thing === "string") {
        // base64url to base64
        thing = thing.replace(/-/g, "+").replace(/_/g, "/");

        // base64 to Uint8Array
        var str = window.atob(thing);
        var bytes = new Uint8Array(str.length);
        for (var i = 0; i < str.length; i++) {
            bytes[i] = str.charCodeAt(i);
        }
        thing = bytes;
    }

    // Array to Uint8Array
    if (Array.isArray(thing)) {
        thing = new Uint8Array(thing);
    }

    // Uint8Array to ArrayBuffer
    if (thing instanceof Uint8Array) {
        thing = thing.buffer;
    }

    // error if none of the above worked
    if (!(thing instanceof ArrayBuffer)) {
        throw new TypeError("could not coerce '" + name + "' to ArrayBuffer");
    }

    return thing;
};

coerceToBase64Url = function (thing) {
    // Array or ArrayBuffer to Uint8Array
    if (Array.isArray(thing)) {
        thing = Uint8Array.from(thing);
    }

    if (thing instanceof ArrayBuffer) {
        thing = new Uint8Array(thing);
    }

    // Uint8Array to base64
    if (thing instanceof Uint8Array) {
        var str = "";
        var len = thing.byteLength;

        for (var i = 0; i < len; i++) {
            str += String.fromCharCode(thing[i]);
        }
        thing = window.btoa(str);
    }

    if (typeof thing !== "string") {
        throw new Error("could not coerce to string");
    }

    // base64 to base64url
    // NOTE: "=" at the end of challenge is optional, strip it off here
    thing = thing.replace(/\+/g, "-").replace(/\//g, "_").replace(/=*$/g, "");

    return thing;
};
