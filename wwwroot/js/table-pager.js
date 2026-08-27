// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
// Pager generik: kelompokkan <tr> di dalam tbody per pageSize baris,
// tampilkan satu halaman aja, sisanya disembunyikan.
// Pager generik: kelompokkan <tr> di dalam tbody per pageSize baris,
// tampilkan satu halaman aja, sisanya disembunyikan.
function initTablePager(options) {
    var tbody = document.querySelector(options.tbodySelector);
    var navWrap = document.getElementById(options.navId);
    if (!navWrap) return;

    var pageSize = options.pageSize || 10;
    var rows = tbody ? Array.from(tbody.querySelectorAll('tr')) : [];

    var input = navWrap.querySelector('.sv-table-nav__input');
    var totalLabel = navWrap.querySelector('.sv-table-nav__total');
    var btnPrev = navWrap.querySelector('.sv-table-nav__prev');
    var btnNext = navWrap.querySelector('.sv-table-nav__next');

    // Kalau gak ada baris sama sekali (misal tabel kosong), tetap tampilkan nav tapi full disable
    var totalPages = rows.length === 0 ? 1 : Math.ceil(rows.length / pageSize);
    var disabled = totalPages <= 1;

    input.max = totalPages;
    totalLabel.textContent = totalPages;
    input.disabled = disabled;

    var current = 0;

    function render() {
        rows.forEach(function (row, idx) {
            var page = Math.floor(idx / pageSize);
            row.style.display = (page === current) ? '' : 'none';
        });
        input.value = current + 1;
        btnPrev.disabled = disabled || current === 0;
        btnNext.disabled = disabled || current === totalPages - 1;
    }

    function goToInputValue() {
        if (disabled) return;
        var target = parseInt(input.value, 10);
        if (isNaN(target)) { render(); return; }
        target = Math.max(1, Math.min(totalPages, target));
        current = target - 1;
        render();
    }

    btnPrev.addEventListener('click', function () {
        if (!disabled && current > 0) { current--; render(); }
    });
    btnNext.addEventListener('click', function () {
        if (!disabled && current < totalPages - 1) { current++; render(); }
    });
    input.addEventListener('keydown', function (e) {
        if (e.key === 'Enter') { e.preventDefault(); goToInputValue(); input.blur(); }
    });
    input.addEventListener('blur', goToInputValue);

    render();
}